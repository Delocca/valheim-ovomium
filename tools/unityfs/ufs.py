"""Minimal UnityFS + SerializedFile (typetree) reader, lazy block decompression.

Usage (python3 with lz4):
    b = Bundle('<game>/valheim_Data/StreamingAssets/SoftRef/Bundles/<hash>')
    sf = SFile(b, b.nodes[0])
    for obj in sf.objects: sf.classid(obj), sf.parse(obj)   # dict par typetree
Bundle d'un asset : SoftRef/manifest_extended. commonstrings.bin = table des chaînes communes Unity (typetree).
"""
import struct, lz4.block, lzma, io, sys, os

COMMON = open(os.path.join(os.path.dirname(__file__), 'commonstrings.bin'), 'rb').read()

def cstr(f):
    b = bytearray()
    while True:
        c = f.read(1)
        if not c or c == b'\0': return b.decode('utf8', 'replace')
        b += c

def decomp(data, flag, usize):
    c = flag & 0x3F
    if c == 0: return data
    if c in (2, 3): return lz4.block.decompress(data, uncompressed_size=usize)
    if c == 1:
        props = data[:5]
        dec = lzma.LZMADecompressor(format=lzma.FORMAT_RAW, filters=[lzma._decode_filter_properties(lzma.FILTER_LZMA1, props)])
        return dec.decompress(data[5:], usize)
    raise Exception('comp %d' % c)

class Bundle:
    def __init__(self, path):
        self.f = f = open(path, 'rb')
        sig = cstr(f); ver = struct.unpack('>I', f.read(4))[0]
        cstr(f); self.unityrev = cstr(f)
        size, csz, usz, flags = struct.unpack('>qIII', f.read(20))
        if ver >= 7: f.seek((f.tell() + 15) // 16 * 16)
        if flags & 0x80:
            pos = f.tell(); f.seek(size - csz); bi = f.read(csz); f.seek(pos)
        else:
            bi = f.read(csz)
        bi = decomp(bi, flags, usz)
        r = io.BytesIO(bi); r.read(16)
        n = struct.unpack('>i', r.read(4))[0]
        self.blocks = []
        for _ in range(n):
            u, c, fl = struct.unpack('>IIH', r.read(10)); self.blocks.append((u, c, fl))
        n = struct.unpack('>i', r.read(4))[0]
        self.nodes = []
        for _ in range(n):
            off, sz, fl = struct.unpack('>qqI', r.read(20)); self.nodes.append((off, sz, fl, cstr(r)))
        if flags & 0x200: f.seek((f.tell() + 15) // 16 * 16)
        self.data_start = f.tell()
        # index
        self.bidx = []; uo = 0; co = self.data_start
        for u, c, fl in self.blocks:
            self.bidx.append((uo, u, co, c, fl)); uo += u; co += c
        self.cache = {}

    def _block(self, i):
        if i in self.cache: return self.cache[i]
        uo, u, co, c, fl = self.bidx[i]
        self.f.seek(co); d = decomp(self.f.read(c), fl, u)
        if len(self.cache) > 64: self.cache.clear()
        self.cache[i] = d; return d

    def read(self, off, n):
        import bisect
        out = bytearray()
        starts = [b[0] for b in self.bidx] if not hasattr(self, '_starts') else self._starts
        self._starts = starts
        while n > 0:
            i = bisect.bisect_right(starts, off) - 1
            d = self._block(i); rel = off - starts[i]
            chunk = d[rel:rel + n]; out += chunk; off += len(chunk); n -= len(chunk)
            if not chunk: raise Exception('eof')
        return bytes(out)

class Node:
    __slots__ = ('type', 'name', 'size', 'flags', 'level', 'children')

class SFile:
    def __init__(self, bundle, node):
        self.b = bundle; self.base = node[0]
        hdr = bundle.read(self.base, 64)
        ver = struct.unpack('>I', hdr[8:12])[0]
        assert ver >= 22, ver
        msize, fsize, doff = struct.unpack('>IqQ', hdr[20:40])
        self.doff = doff
        meta = io.BytesIO(bundle.read(self.base + 48, msize))
        self.le = '<'
        rd = lambda fmt: struct.unpack('<' + fmt, meta.read(struct.calcsize('<' + fmt)))
        self.uver = cstr(meta); self.platform = rd('i')[0]; tt = rd('?')[0]
        self.types = []
        for _ in range(rd('i')[0]):
            cid, stripped, sidx = rd('i?h')
            if cid == 114 or cid < 0 or sidx >= 0: meta.read(16)
            meta.read(16)
            root = None
            if tt:
                nn, sb = rd('ii')
                raw = [rd('HBBIIiiiQ') for _ in range(nn)]
                sbuf = meta.read(sb)
                def s(o):
                    buf, o = (COMMON, o & 0x7FFFFFFF) if o & 0x80000000 else (sbuf, o)
                    return buf[o:buf.index(b'\0', o)].decode()
                nodes = []
                for (v, lvl, tf, to, no, bs, idx, mf, rh) in raw:
                    n = Node(); n.type = s(to); n.name = s(no); n.size = bs; n.flags = mf; n.level = lvl; n.children = []
                    nodes.append(n)
                stack = []
                for n in nodes:
                    while stack and stack[-1].level >= n.level: stack.pop()
                    if stack: stack[-1].children.append(n)
                    stack.append(n)
                root = nodes[0]
                nd = rd('i')[0]; meta.read(4 * nd)
            self.types.append((cid, root))
        self.objects = []
        for _ in range(rd('i')[0]):
            meta.seek((meta.tell() + 3) // 4 * 4)
            pid, start, size, tid = rd('qqIi')
            self.objects.append((pid, start, size, tid))

    def raw(self, obj):
        return self.b.read(self.base + self.doff + obj[1], obj[2])

    def classid(self, obj): return self.types[obj[3]][0]

    def parse(self, obj, skip_big=True):
        data = self.raw(obj); r = Reader(data)
        return r.read_node(self.types[obj[3]][1], skip_big)

PRIM = {'SInt8': 'b', 'UInt8': 'B', 'char': 'B', 'bool': '?', 'SInt16': 'h', 'short': 'h', 'UInt16': 'H', 'unsigned short': 'H',
        'SInt32': 'i', 'int': 'i', 'UInt32': 'I', 'unsigned int': 'I', 'Type*': 'I', 'SInt64': 'q', 'long long': 'q', 'UInt64': 'Q',
        'unsigned long long': 'Q', 'FileSize': 'Q', 'float': 'f', 'double': 'd'}

class Reader:
    def __init__(self, d): self.d = d; self.p = 0
    def u(self, fmt):
        v = struct.unpack_from('<' + fmt, self.d, self.p); self.p += struct.calcsize('<' + fmt); return v[0]
    def align(self): self.p = (self.p + 3) // 4 * 4
    def read_node(self, n, skip_big):
        t = n.type
        if t in PRIM: v = self.u(PRIM[t])
        elif t == 'string':
            l = self.u('i'); v = self.d[self.p:self.p + l].decode('utf8', 'replace'); self.p += l
            if n.children and n.children[0].flags & 0x4000: self.align()
        elif t == 'TypelessData':
            l = self.u('i'); v = ('<bytes %d>' % l) if skip_big else self.d[self.p:self.p + l]; self.p += l
        elif n.children and n.children[0].type == 'Array' or t == 'Array':
            arr = n if t == 'Array' else n.children[0]
            cnt = self.u('i'); el = arr.children[1]
            if el.type in ('UInt8', 'char', 'SInt8') and not el.children:
                v = self.d[self.p:self.p + cnt]; self.p += cnt
                if skip_big and cnt > 256: v = '<bytes %d>' % cnt
            elif el.type in PRIM and not el.children:
                fmt = PRIM[el.type]; v = list(struct.unpack_from('<%d%s' % (cnt, fmt), self.d, self.p)); self.p += cnt * struct.calcsize(fmt)
            else:
                v = [self.read_node(el, skip_big) for _ in range(cnt)]
            if arr.flags & 0x4000: self.align()
        else:
            v = {}
            for c in n.children: v[c.name] = self.read_node(c, skip_big)
        if n.flags & 0x4000: self.align()
        return v
