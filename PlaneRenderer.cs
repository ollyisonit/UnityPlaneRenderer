#pragma warning disable 0649

using dninosores.UnityEditorAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace dninosores.UnityPlaneRenderer
{
	/// <summary>
	/// Renders a two-dimensional image to a plane using given material as base.
	/// </summary>
	public class PlaneRenderer : MonoBehaviour
	{
		private const float OFFSET = 0.032f;

		/// <summary>
		/// Image to display.
		/// </summary>
		[SerializeField, Tooltip("Texture to display")]
		private Texture2D image;
		/// <summary>
		/// How many pixels in the image should be taken as one meter in world space?
		/// </summary>
		[SerializeField, Tooltip("How many pixels in the image should be taken as one meter in world space?")]
		private float pixelsPerMeter;
		/// <summary>
		/// Where transform anchor will be. (0, 0) is bottom left, (1, 1) is top right.
		/// </summary>
		[SerializeField, Tooltip("Where transform anchor will be. (0, 0) is bottom left, (1, 1) is top right.")]
		private Vector2 anchorPoint;
		/// <summary>
		/// Material to use for rendering image. Material will be copied on use, image will be applied to shader's main texture.
		/// </summary>
		[SerializeField, Tooltip(" Material to use for rendering image. " +
			"Material will be copied on use, image will be applied to shader's main texture.")]
		private Material material;
		/// <summary>
		/// Can object only be seen from the front?
		/// </summary>
		[SerializeField, Tooltip("Can object only be seen from the front?")]
		private bool oneSided;
		/// <summary>
		/// Image to display.
		/// </summary>
		public Texture2D Image
		{
			get => image;
			set { image = value; Recalculate(); }
		}
		/// <summary>
		/// How many pixels in the image should be taken as one meter in world space?
		/// </summary>
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
		public Material Material
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
		[SerializeField, Tooltip("How should shadows be handled?")]
		private ShadowCastingMode castShadows;

		// These classes must be referenced to prevent build errors with CreatePrimitive.
		private MeshFilter insuranceFilter;
		private MeshRenderer insuranceRenderer;
		private BoxCollider insuranceBox;
		private SphereCollider insuranceSphere;

		[SerializeField, ReadOnly, Tooltip("GameObject used for the front plane")]
		private GameObject front;
		[SerializeField, ReadOnly, Tooltip("GameObject used for the back plane")]
		private GameObject back;


		/// <summary>
		/// Creates a colliderless Quad with the given name as a child of this GameObject.
		/// </summary>
		private GameObject CreateQuad(string name)
		{
			GameObject o = GameObject.CreatePrimitive(PrimitiveType.Quad);
			o.name = name;
			o.transform.SetParent(transform);
			SafeDestroy(o.GetComponent<MeshCollider>());
			o.GetComponent<MeshRenderer>().shadowCastingMode = castShadows;
			return o;
		}


		/// <summary>
		/// Destroys a GameObject's material and resets its rotation.
		/// </summary>
		private void Clean(GameObject o)
		{
			SafeDestroy(o.GetComponent<Renderer>().sharedMaterial);
			o.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
		}


		/// <summary>
		/// Destroys all children of this GameObject that aren't the front or back quads.
		/// </summary>
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


		/// <summary>
		/// Sets the GameObject's texture to be the given image.
		/// </summary>
		/// <param name="o"></param>
		private void SetTexture(GameObject o)
		{
			Renderer rend = o.GetComponent<Renderer>();
			rend.material = Instantiate(material);
			rend.sharedMaterial.mainTexture = image;
		}


		/// <summary>
		/// Recalculates the front and back quads.
		/// </summary>
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


		/// <summary>
		/// Recalculates the dimensions of the front and back quads, assuming they already exist.
		/// </summary>
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


		/// <summary>
		/// Recalculates dimensions when editor values change.
		/// </summary>
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


		/// <summary>
		/// Calls appropriate destroy method depending on whether game is running or not.
		/// </summary>
		/// <param name="go"></param>
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
