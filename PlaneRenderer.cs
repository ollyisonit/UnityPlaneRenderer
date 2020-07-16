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

		[SerializeField, Tooltip("Texture to display on front")]
		private Texture2D frontImage;
		[SerializeField, Tooltip("Texture to display on back")]
		private Texture2D backImage;
		[SerializeField, Tooltip("Should back image be mirrored?")]
		private bool mirrorBackImage;
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
		/// Image to display on front.
		/// </summary>
		public Texture2D FrontImage
		{
			get => frontImage;
			set
			{
				frontImage = value;
				Recalculate();
			}
		}
		/// <summary>
		/// Image to display on back.
		/// </summary>
		public Texture2D BackImage
		{
			get => backImage;
			set
			{
				backImage = value;
				Recalculate();
			}
		}
		/// <summary>
		/// How many pixels in the image should be taken as one meter in world space?
		/// </summary>
		public float PixelsPerMeter
		{
			get => PixelsPerMeter;
			set
			{
				pixelsPerMeter = value;
				Recalculate();
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
				anchorPoint = value;
				Recalculate();
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
				material = value;
				Recalculate();
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
			if (o != null)
			{
				SafeDestroy(o.GetComponent<Renderer>().sharedMaterial);
				o.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
			}
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
		private void SetTexture(GameObject o, Texture2D tex)
		{
			Renderer rend = o.GetComponent<Renderer>();
			rend.material = Instantiate(material);
			rend.sharedMaterial.mainTexture = tex;
		}


		/// <summary>
		/// Recalculates the front and back quads.
		/// </summary>
		[ContextMenu("Recalculate")]
		private void Recalculate()
		{
			CleanChildren();
			if (material == null)
			{
				SafeDestroy(front);
				SafeDestroy(back);
			}
			if (frontImage == null)
			{
				SafeDestroy(front);
			}
			if (backImage == null)
			{
				SafeDestroy(back);
			}

			if (front == null && frontImage != null)
			{
				front = CreateQuad("front");
			}
			else
			{
				Clean(front);
			}

			if (back == null && backImage != null)
			{
				back = CreateQuad("back");
			}
			else
			{
				Clean(back);
			}


			if (frontImage != null)
			{
				RecalculateDimensions(frontImage, front, false, 180);
			}
			if (backImage != null)
			{
				RecalculateDimensions(backImage, back, mirrorBackImage, 0);
			}


		}


		/// <summary>
		/// Recalculates the dimensions of the front and back quads, assuming they already exist.
		/// </summary>
		private void RecalculateDimensions(Texture2D image, GameObject plane, bool flip, float rotation)
		{
			float width = image.width / pixelsPerMeter;
			float height = image.height / pixelsPerMeter;
			plane.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
			plane.transform.Rotate(new Vector3(0, rotation, 0));

			plane.transform.localScale = new Vector3(width * (flip ? -1 : 1), height, 1);

			SetTexture(plane, image);

			plane.transform.localPosition = new Vector3(-width / 2 + anchorPoint.x * width, height / 2 - anchorPoint.y * height, OFFSET);
			plane.GetComponent<MeshRenderer>().shadowCastingMode = castShadows;

#if UNITY_EDITOR
			EditorUtility.SetDirty(plane);

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
			if (material != null)
			{
				if (front != null && frontImage != null)
				{
					RecalculateDimensions(frontImage, front, false, 180);
				}
				if (back != null && backImage != null)
				{
					RecalculateDimensions(backImage, back, mirrorBackImage, 0);
				}
				if ((front == null && frontImage != null) || (back == null && backImage != null))
				{
					Recalculate();
				}
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
