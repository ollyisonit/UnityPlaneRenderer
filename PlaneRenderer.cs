using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace dninosores.UnityPlaneRenderer
{
#if UNITY_EDITOR
	public class ReadOnlyAttribute : PropertyAttribute
	{

	}

	[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public class ReadOnlyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property,
												GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI(Rect position,
								   SerializedProperty property,
								   GUIContent label)
		{
			GUI.enabled = false;
			EditorGUI.PropertyField(position, property, label, true);
			GUI.enabled = true;
		}
	}
#endif
	/// <summary>
	/// Renders a two-dimensional image to a plane using given material as base.
	/// </summary>
	public class PlaneRenderer : MonoBehaviour
	{
		private const float OFFSET = 0.032f;

		/// <summary>
		/// Image to display.
		/// </summary>
		[SerializeField]
		private Texture2D image;
		/// <summary>
		/// How many pixels in the image should be taken as one meter in world space?
		/// </summary>
		[SerializeField]
		private float pixelsPerMeter;
		/// <summary>
		/// Where transform gimbal will be. (0, 0) is bottom left, (1, 1) is top right.
		/// </summary>
		[SerializeField]
		private Vector2 anchorPoint;
		/// <summary>
		/// Material to use for rendering image. Material will be copied on use, image will be applied to shader's main texture.
		/// </summary>
		[SerializeField]
		private Material material;
		/// <summary>
		/// Can object only be seen from the front?
		/// </summary>
		[SerializeField]
		private bool oneSided;
		/// <summary>
		/// Image to display.
		/// </summary>
		[SerializeField]
		public Texture2D Image
		{
			get => image;
			set { image = value; Recalculate(); }
		}
		/// <summary>
		/// How many pixels in the image should be taken as one meter in world space?
		/// </summary>
		[SerializeField]
		public float PixelsPerMeter
		{
			get => PixelsPerMeter;
			set
			{
				pixelsPerMeter = value; Recalculate();
			}
		}
		/// <summary>
		/// Where transform gimbal will be. (0, 0) is bottom left, (1, 1) is top right.
		/// </summary>
		[SerializeField]
		public Vector2 AnchorPoint
		{
			get => anchorPoint;
			set
			{
				anchorPoint = value; Recalculate();
			}
		}
		/// <summary>
		/// Material to use for rendering image. Material will be copied on use, image will be applied to shader's main texture.
		/// </summary>
		[SerializeField]
		private Material Material
		{
			get => material;
			set
			{
				material = value; Recalculate();
			}
		}

		/// <summary>
		/// Should the plane cast shadows?
		/// </summary>
		[SerializeField]
		private ShadowCastingMode castShadows;

		// These classes must be referenced to prevent build errors with CreatePrimitive.
		private MeshFilter insuranceFilter;
		private MeshRenderer insuranceRenderer;
		private BoxCollider insuranceBox;
		private SphereCollider insuranceSphere;

		[SerializeField
			#if UNITY_EDITOR
			,ReadOnly
			#endif
			]
		private GameObject front;
		[SerializeField
#if UNITY_EDITOR
			, ReadOnly
			#endif
			]
		private GameObject back;


		private GameObject CreateQuad(string name)
		{
			GameObject o = GameObject.CreatePrimitive(PrimitiveType.Quad);
			o.name = name;
			o.transform.SetParent(transform);
			SafeDestroy(o.GetComponent<MeshCollider>());
			o.GetComponent<MeshRenderer>().shadowCastingMode = castShadows;
			return o;
		}


		private void Clean(GameObject o)
		{
			SafeDestroy(o.GetComponent<Renderer>().sharedMaterial);
			o.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
		}


		private void CleanChildren()
		{
			foreach (Transform child in transform)
			{
				if (child != null &&
					(front == null || child != front?.transform) &&
					(back == null || child != back?.transform))
				{
					SafeDestroy(child?.gameObject);
				}
			}
		}


		private void SetTexture(GameObject o)
		{
			Renderer rend = o.GetComponent<Renderer>();
			rend.material = Instantiate(material);
			rend.sharedMaterial.mainTexture = image;
		}


		[ContextMenu("Recalculate")]
		private void Recalculate()
		{
			CleanChildren();
			if (image == null || material == null)
			{
				SafeDestroy(front);
				SafeDestroy(back);
			}
			else
			{
				if (front == null)
				{
					front = CreateQuad("front");
				}
				else
				{
					Clean(front);
				}
				if (!oneSided)
				{
					if (back == null)
					{
						back = CreateQuad("back");
					}
					else
					{
						Clean(back);
					}
				}
				else
				{
					if (back != null)
					{
						SafeDestroy(back);
					}
				}

				RecalculateDimensions();

			}
		}


		private void RecalculateDimensions()
		{
			float width = image.width / pixelsPerMeter;
			float height = image.height / pixelsPerMeter;
			front.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
			front.transform.Rotate(new Vector3(0, 180, 0));

			front.transform.localScale = new Vector3(-width, height, 1);
			
			SetTexture(front);
			
			front.transform.localPosition = new Vector3(-width / 2 + anchorPoint.x * width, height / 2 - anchorPoint.y * height, OFFSET);
			front.GetComponent<MeshRenderer>().shadowCastingMode = castShadows;
			if (!oneSided)
			{
				back.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
				
				back.transform.localScale = new Vector3(width, height, 1);
				SetTexture(back);
				back.transform.localPosition = new Vector3(-width / 2 + anchorPoint.x * width, height / 2 - anchorPoint.y * height, -OFFSET);
				back.GetComponent<MeshRenderer>().shadowCastingMode = castShadows;
#if UNITY_EDITOR
				EditorUtility.SetDirty(back);
#endif
			}

#if UNITY_EDITOR
			EditorUtility.SetDirty(front);
		
			EditorUtility.SetDirty(this.gameObject);
#endif
		}


		void OnValidate()
		{
			if (pixelsPerMeter <= 0)
			{
				pixelsPerMeter = 0.001f;
			}
			if (front != null && (back != null || oneSided) && image != null && material != null)
			{
				RecalculateDimensions();
			}
		}


		private void SafeDestroy(UnityEngine.Object go)
		{
			if (go == null)
			{
				return;
			}
#if UNITY_EDITOR
			if (!EditorApplication.isPlaying)
			UnityEditor.EditorApplication.delayCall += () =>
			{
				DestroyImmediate(go, false);
			};
			else
#endif
				Destroy(go);
		}

	}
}
