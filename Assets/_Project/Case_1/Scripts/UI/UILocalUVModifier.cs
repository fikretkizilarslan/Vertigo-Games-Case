using UnityEngine;
using UnityEngine.UI;

namespace BattlePass.UI
{
    [ExecuteAlways]
    [AddComponentMenu("UI/Effects/UI Local UV Modifier")]
    public class UILocalUVModifier : MonoBehaviour, IMeshModifier
    {
        private Graphic m_Graphic;

        protected void OnEnable()
        {
            m_Graphic = GetComponent<Graphic>();
            if (m_Graphic != null)
            {
                m_Graphic.SetVerticesDirty();
            }
        }

        protected void OnDisable()
        {
            if (m_Graphic != null)
            {
                m_Graphic.SetVerticesDirty();
            }
        }

        protected void OnValidate()
        {
            if (m_Graphic != null)
            {
                m_Graphic.SetVerticesDirty();
            }
        }

        public void ModifyMesh(Mesh mesh)
        {
            // Not used in UGUI Graphic pipeline (ModifyMesh(VertexHelper vh) is used instead)
        }

        public void ModifyMesh(VertexHelper vh)
        {
            if (!enabled || vh == null) return;
            
            int count = vh.currentVertCount;
            if (count == 0) return;

            UIVertex vertex = new UIVertex();
            
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                minX = Mathf.Min(minX, vertex.position.x);
                maxX = Mathf.Max(maxX, vertex.position.x);
                minY = Mathf.Min(minY, vertex.position.y);
                maxY = Mathf.Max(maxY, vertex.position.y);
            }
            
            float width = maxX - minX;
            float height = maxY - minY;
            
            if (width <= 0f) width = 1f;
            if (height <= 0f) height = 1f;
            
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                
                float localX = (vertex.position.x - minX) / width;
                float localY = (vertex.position.y - minY) / height;
                
                vertex.uv1 = new Vector2(localX, localY);
                
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
