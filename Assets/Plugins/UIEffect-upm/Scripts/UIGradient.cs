using UnityEngine;
using UnityEngine.UI;

namespace Coffee.UIEffects
{
    /// <summary>
    /// UIGradient.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/UIEffects/UIGradient", 101)]
    public class UIGradient : BaseMeshEffect
    {
        static readonly Vector2[] s_SplitedCharacterPosition = { Vector2.up, Vector2.one, Vector2.right, Vector2.zero };

        /// <summary>
        /// Gradient direction.
        /// </summary>
        public enum Direction
        {
            Horizontal,
            Vertical,
            Angle,
            Diagonal,
        }

        /// <summary>
        /// Gradient space for Text.
        /// </summary>
        public enum GradientStyle
        {
            Rect,
            Fit,
            Split,
        }


        [Tooltip("Gradient Direction.")]
        [SerializeField]
        Direction m_Direction;

        [Tooltip("Color1: Top or Left.")]
        [SerializeField]
        Color m_Color1 = Color.white;

        [Tooltip("Color2: Bottom or Right.")]
        [SerializeField]
        Color m_Color2 = Color.white;

        [Tooltip("Color3: For diagonal.")]
        [SerializeField]
        Color m_Color3 = Color.white;

        [Tooltip("Color4: For diagonal.")]
        [SerializeField]
        Color m_Color4 = Color.white;

        [Tooltip("Gradient rotation.")]
        [SerializeField]
        [Range(-180, 180)]
        float m_Rotation;

        [Tooltip("Gradient offset for Horizontal, Vertical or Angle.")]
        [SerializeField]
        [Range(-1, 1)]
        float m_Offset1;

        [Tooltip("Gradient offset for Diagonal.")]
        [SerializeField]
        [Range(-1, 1)]
        float m_Offset2;

        [Tooltip("Gradient style for Text.")]
        [SerializeField]
        GradientStyle m_GradientStyle;

        [Tooltip("Color space to correct color.")]
        [SerializeField]
        ColorSpace m_ColorSpace = ColorSpace.Uninitialized;

        [Tooltip("Ignore aspect ratio.")]
        [SerializeField]
        bool m_IgnoreAspectRatio = true;

        /// <summary>
        /// Gradient Direction.
        /// </summary>
        public Direction direction
        {
            get { return m_Direction; }
            set
            {
                if (m_Direction == value) return;
                m_Direction = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Color1: Top or Left.
        /// </summary>
        public Color color1
        {
            get { return m_Color1; }
            set
            {
                if (m_Color1 == value) return;
                m_Color1 = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Color2: Bottom or Right.
        /// </summary>
        public Color color2
        {
            get { return m_Color2; }
            set
            {
                if (m_Color2 == value) return;
                m_Color2 = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Color3: For diagonal.
        /// </summary>
        public Color color3
        {
            get { return m_Color3; }
            set
            {
                if (m_Color3 == value) return;
                m_Color3 = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Color4: For diagonal.
        /// </summary>
        public Color color4
        {
            get { return m_Color4; }
            set
            {
                if (m_Color4 == value) return;
                m_Color4 = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Gradient rotation.
        /// </summary>
        public float rotation
        {
            get
            {
                return m_Direction == Direction.Horizontal ? -90
                    : m_Direction == Direction.Vertical ? 0
                    : m_Rotation;
            }
            set
            {
                if (Mathf.Approximately(m_Rotation, value)) return;
                m_Rotation = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Gradient offset for Horizontal, Vertical or Angle.
        /// </summary>
        public float offset
        {
            get { return m_Offset1; }
            set
            {
                if (Mathf.Approximately(m_Offset1, value)) return;
                m_Offset1 = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Gradient offset for Diagonal.
        /// </summary>
        public Vector2 offset2
        {
            get { return new Vector2(m_Offset2, m_Offset1); }
            set
            {
                if (Mathf.Approximately(m_Offset1, value.y) && Mathf.Approximately(m_Offset2, value.x)) return;
                m_Offset1 = value.y;
                m_Offset2 = value.x;
                SetVerticesDirty();
            }
        }



        /// <summary>
        /// Gradient style for Text.
        /// </summary>
        public GradientStyle gradientStyle
        {
            get { return m_GradientStyle; }
            set
            {
                if (m_GradientStyle == value) return;
                m_GradientStyle = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Color space to correct color.
        /// </summary>
        public ColorSpace colorSpace
        {
            get { return m_ColorSpace; }
            set
            {
                if (m_ColorSpace == value) return;
                m_ColorSpace = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Ignore aspect ratio.
        /// </summary>
        public bool ignoreAspectRatio
        {
            get { return m_IgnoreAspectRatio; }
            set
            {
                if (m_IgnoreAspectRatio == value) return;
                m_IgnoreAspectRatio = value;
                SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh, Graphic graphic)
        {
            if (!isActiveAndEnabled)
                return;

            // Gradient space.
            var rect = CalculateGradientRect(vh, graphic);

            // Gradient rotation.
            var dir = CalculateDirection(rect);

            // Calculate vertex color.
            ApplyGradientColor(vh, rect, dir);
        }

        private Rect CalculateGradientRect(VertexHelper vh, Graphic graphic)
        {
            var rect = default(Rect);
            var vertex = default(UIVertex);
            switch (m_GradientStyle)
            {
                case GradientStyle.Rect:
                    rect = graphic.rectTransform.rect;
                    break;
                case GradientStyle.Split:
                    rect.Set(0, 0, 1, 1);
                    break;
                case GradientStyle.Fit:
                    rect.xMin = rect.yMin = float.MaxValue;
                    rect.xMax = rect.yMax = float.MinValue;
                    for (var i = 0; i < vh.currentVertCount; i++)
                    {
                        vh.PopulateUIVertex(ref vertex, i);
                        rect.xMin = Mathf.Min(rect.xMin, vertex.position.x);
                        rect.yMin = Mathf.Min(rect.yMin, vertex.position.y);
                        rect.xMax = Mathf.Max(rect.xMax, vertex.position.x);
                        rect.yMax = Mathf.Max(rect.yMax, vertex.position.y);
                    }
                    break;
            }
            return rect;
        }

        private Vector2 CalculateDirection(Rect rect)
        {
            var rad = rotation * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            if (!m_IgnoreAspectRatio && Direction.Angle <= m_Direction)
            {
                dir.x *= rect.height / rect.width;
                dir = dir.normalized;
            }
            return dir;
        }

        private void ApplyGradientColor(VertexHelper vh, Rect rect, Vector2 dir)
        {
            var localMatrix = new Matrix2x3(rect, dir.x, dir.y);
            var vertex = default(UIVertex);

            for (var i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);

                // Normalize vertex position by local matrix.
                Vector2 normalizedPos = (m_GradientStyle == GradientStyle.Split)
                    ? localMatrix * s_SplitedCharacterPosition[i % 4] + offset2
                    : localMatrix * vertex.position + offset2;

               // Interpolate vertex color.
               Color color = (direction == Direction.Diagonal)
                    ? Color.LerpUnclamped(
                        Color.LerpUnclamped(m_Color1, m_Color2, normalizedPos.x),
                        Color.LerpUnclamped(m_Color3, m_Color4, normalizedPos.x),
                        normalizedPos.y)
                    : Color.LerpUnclamped(m_Color2, m_Color1, normalizedPos.y);

                //color = Color.red;
                // Correct color.
                vertex.color *= (m_ColorSpace == ColorSpace.Gamma) ? color.gamma
                    : (m_ColorSpace == ColorSpace.Linear) ? color.linear
                    : color;

                vh.SetUIVertex(vertex, i);
            }
        }

    }
}
