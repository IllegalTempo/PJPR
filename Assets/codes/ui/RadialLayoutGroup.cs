using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Assets.codes.ui
{
	[ExecuteAlways]
	public class RadialLayoutGroup : LayoutGroup
	{
		[SerializeField]
		private float _radius = 100f;

		[SerializeField]
		[Range(0f, 360f)]
		private float _startAngle = 0f;

		[SerializeField]
		[Range(0f, 360f)]
		private float _maxAngle = 360f;

		[SerializeField]
		private bool _clockwise = true;

		[SerializeField]
		private bool _evenly = true;

		[SerializeField]
		private float _spacing = 0f;

		[SerializeField]
		private Vector2 _childSize = new Vector2(50f, 50f);

		[SerializeField]
		private bool _controlChildSize = true;

		public float Radius
		{
			get => _radius;
			set
			{
				_radius = value;
				SetDirty();
			}
		}

		public float StartAngle
		{
			get => _startAngle;
			set
			{
				_startAngle = value;
				SetDirty();
			}
		}

		public float MaxAngle
		{
			get => _maxAngle;
			set
			{
				_maxAngle = Mathf.Clamp(value, 0f, 360f);
				SetDirty();
			}
		}

		public bool Clockwise
		{
			get => _clockwise;
			set
			{
				_clockwise = value;
				SetDirty();
			}
		}

		public bool Evenly
		{
			get => _evenly;
			set
			{
				_evenly = value;
				SetDirty();
			}
		}

		public float Spacing
		{
			get => _spacing;
			set
			{
				_spacing = value;
				SetDirty();
			}
		}

		public Vector2 ChildSize
		{
			get => _childSize;
			set
			{
				_childSize = value;
				SetDirty();
			}
		}

		public bool ControlChildSize
		{
			get => _controlChildSize;
			set
			{
				_controlChildSize = value;
				SetDirty();
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			CalculateRadial();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			CalculateRadial();
		}
#endif

		public override void SetLayoutHorizontal()
		{
			CalculateRadial();
		}

		public override void SetLayoutVertical()
		{
		}

		public override void CalculateLayoutInputVertical()
		{
			CalculateRadial();
		}

		public override void CalculateLayoutInputHorizontal()
		{
			CalculateRadial();
		}

		private void CalculateRadial()
		{
			m_Tracker.Clear();

			if (transform.childCount == 0)
				return;

			List<RectTransform> children = new List<RectTransform>();

			for (int i = 0; i < transform.childCount; i++)
			{
				RectTransform child = transform.GetChild(i) as RectTransform;
				if (child != null && child.gameObject.activeSelf)
				{
					children.Add(child);
				}
			}

			if (children.Count == 0)
				return;

			float angleStep;
			if (_evenly)
			{
				angleStep = children.Count > 1 ? _maxAngle / children.Count : 0f;
			}
			else
			{
				angleStep = children.Count > 1 ? (_maxAngle - _spacing * (children.Count - 1)) / children.Count : 0f;
			}

			float currentAngle = _startAngle;

			for (int i = 0; i < children.Count; i++)
			{
				RectTransform child = children[i];

				m_Tracker.Add(this, child, DrivenTransformProperties.Anchors | 
					DrivenTransformProperties.AnchoredPosition | 
					DrivenTransformProperties.Pivot);

				child.anchorMin = new Vector2(0.5f, 0.5f);
				child.anchorMax = new Vector2(0.5f, 0.5f);
				child.pivot = new Vector2(0.5f, 0.5f);

				float angle = currentAngle;
				if (!_clockwise)
				{
					angle = -angle;
				}

				float radian = angle * Mathf.Deg2Rad;
				float x = Mathf.Cos(radian) * _radius;
				float y = Mathf.Sin(radian) * _radius;

				child.anchoredPosition = new Vector2(x, y);

				if (_controlChildSize)
				{
					m_Tracker.Add(this, child, DrivenTransformProperties.SizeDelta);
					child.sizeDelta = _childSize;
				}

				if (_evenly)
				{
					currentAngle += angleStep;
				}
				else
				{
					currentAngle += angleStep + _spacing;
				}
			}
		}

		private void SetDirty()
		{
			if (!IsActive())
				return;

			LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
		}

#if UNITY_EDITOR
		[ContextMenu("Refresh Layout")]
		private void RefreshLayout()
		{
			CalculateRadial();
		}
#endif
	}
}