using UnityEngine;

namespace Ovomium.Features.ItemFlight
{
    /// <summary>
    /// Un objet en vol : une Bézier cubique dont les deux premiers points sont sur la droite coffre → 50 cm au-dessus
    /// de la cible et le troisième à cette hauteur au-dessus de la cible (ligne quasi droite qui s'incurve à la fin
    /// pour arriver par le haut), vitesse décroissante, légère ondulation verticale et latérale nulle aux extrémités,
    /// rotation lente du mesh (la racine ne tourne pas : la traînée est émise dans l'espace monde), grossissement
    /// au départ et rétrécissement à l'arrivée sur <see cref="ScaleSeconds"/>. À l'arrivée, la traînée est détachée et s'éteint d'elle-même ; à l'annulation, tout disparaît.
    /// </summary>
    internal sealed class FlyingItem : MonoBehaviour
    {
        private const float DiveHeight = 0.5f;
        private const int ArcSamples = 48;
        /// <summary>L'objet grossit depuis rien au départ, et rétrécit jusqu'à disparaître à l'arrivée, sur cette durée.</summary>
        private const float ScaleSeconds = 0.3f;

        private Vector3 m_p0, m_p1, m_p2, m_p3;
        /// <summary>Longueur cumulée de la courbe à chaque échantillon, pour avancer à distance constante.</summary>
        private readonly float[] m_arc = new float[ArcSamples + 1];
        private Vector3 m_side;
        private float m_duration, m_delay, m_elapsed, m_wobble, m_phase;
        private Vector3 m_spinAxis;
        private float m_spinSpeed;
        private Transform m_mesh;
        private Vector3 m_meshScale;
        private GameObject m_trail;
        private bool m_released;

        /// <summary>Vrai tant que le craft qui a lancé ce vol n'a pas consommé ses ingrédients (annulable).</summary>
        public bool FromCraft { get; set; }

        /// <summary><paramref name="phase"/> décale l'ondulation : identique pour plusieurs exemplaires d'un même
        /// ingrédient, ils suivent exactement la même trajectoire, en file.</summary>
        public void Setup(Vector3 from, Vector3 to, float duration, float delay, Transform mesh, GameObject trail, float phase)
        {
            float distance = Vector3.Distance(from, to);
            Vector3 dir = to - from;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
            Vector3 above = to + Vector3.up * DiveHeight;
            m_p0 = from;
            m_p1 = Vector3.Lerp(from, above, 0.75f);
            m_p2 = above;
            m_p3 = to;
            for (int i = 1; i <= ArcSamples; i++)
                m_arc[i] = m_arc[i - 1] + Vector3.Distance(Bezier((i - 1f) / ArcSamples), Bezier((float)i / ArcSamples));
            m_side = Vector3.Cross(dir, Vector3.up);
            m_wobble = Mathf.Clamp(distance * 0.01f, 0.05f, 0.25f);
            m_phase = phase;
            m_spinAxis = Random.onUnitSphere;
            m_spinSpeed = Random.Range(60f, 180f);
            m_duration = duration;
            m_delay = delay;
            m_mesh = mesh;
            m_meshScale = mesh != null ? mesh.localScale : Vector3.one;
            if (mesh != null) mesh.localScale = Vector3.zero;  // invisible jusqu'au départ
            m_trail = trail;
            transform.position = from;
        }

        private void Update()
        {
            if (m_released) return;
            m_elapsed += Time.deltaTime;
            float t = (m_elapsed - m_delay) / m_duration;
            if (t < 0f) return;
            if (t >= 1f) { Release(); return; }
            transform.position = PositionAt(t);
            if (m_mesh != null)
            {
                m_mesh.Rotate(m_spinAxis, m_spinSpeed * Time.deltaTime, Space.World);
                float grow = t * m_duration / ScaleSeconds;
                float shrink = (1f - t) * m_duration / ScaleSeconds;
                m_mesh.localScale = m_meshScale * Mathf.Clamp01(Mathf.Min(grow, shrink));
            }
        }

        /// <summary>
        /// Distance parcourue en ease-out (rapide au départ, 30 % de vitesse résiduelle à l'arrivée, pas de
        /// sur-place), convertie en paramètre de courbe par la table des longueurs, plus l'ondulation.
        /// </summary>
        private Vector3 PositionAt(float t)
        {
            float eased = 0.7f * (1f - (1f - t) * (1f - t)) + 0.3f * t;
            float s = ParameterAtDistance(eased * m_arc[ArcSamples]);
            Vector3 pos = Bezier(s);
            float envelope = Mathf.Sin(Mathf.PI * s);
            pos += Vector3.up * (m_wobble * envelope * Mathf.Sin(s * 6f * Mathf.PI + m_phase));
            pos += m_side * (m_wobble * 0.6f * envelope * Mathf.Sin(s * 4f * Mathf.PI + m_phase * 0.5f));
            return pos;
        }

        private Vector3 Bezier(float s)
        {
            float u = 1f - s;
            return u * u * u * m_p0 + 3f * u * u * s * m_p1 + 3f * u * s * s * m_p2 + s * s * s * m_p3;
        }

        private float ParameterAtDistance(float distance)
        {
            for (int i = 1; i <= ArcSamples; i++)
            {
                if (distance > m_arc[i]) continue;
                float segment = m_arc[i] - m_arc[i - 1];
                float within = segment > 0f ? (distance - m_arc[i - 1]) / segment : 0f;
                return (i - 1 + within) / ArcSamples;
            }
            return 1f;
        }

        /// <summary>Craft interrompu : objet et traînée disparaissent sur-le-champ.</summary>
        public void Cancel()
        {
            if (m_released) return;
            m_released = true;
            Destroy(gameObject);
        }

        /// <summary>Fin de vol : l'objet disparaît, la traînée persiste puis se détruit.</summary>
        public void Release()
        {
            if (m_released) return;
            m_released = true;
            if (m_trail != null)
            {
                m_trail.transform.SetParent(null, true);
                FlightVisuals.StopEmission(m_trail);
                ItemFlight.Linger(m_trail, ItemFlightConfig.TrailLinger.Value);
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            ItemFlight.Forget(this);
        }
    }
}
