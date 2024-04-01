#pragma warning disable 0649

using ollyisonit.UnityEditorAttributes;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ollyisonit.UnityPlaneRenderer
{
	/// <summary>
	/// Renders a two-dimensional image to a plane using given material as base.
	/// </summary>
	[ExecuteInEditMode, DisallowMultipleComponent]
	public class PlaneRenderer : MonoBehaviour
	{

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
		[SerializeField, Tooltip("Material to use for rendering image. " +
			"Material will be copied on use, image will be applied to shader's main texture.")]
		private Material material;

		[SerializeField, Tooltip("Distance between each plane and the center. Setting this number too small can create lighting errors.")]
		private float Offset;


		private HashSet<Object> deletionQueue = new HashSet<Object>();

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


		#region UnityMethods

#if UNITY_EDITOR
		/// <summary>
		/// Recalculates dimensions when editor values change.
		/// </summary>
		void OnValidate()
		{
			if (pixelsPerMeter <= 0)
			{
				pixelsPerMeter = float.Epsilon;
			}
			if (material != null)
			{
				EditorApplication.delayCall += RecalculateExistingFrontAndBack;
			}
		}


		void Reset()
		{
			pixelsPerMeter = 100;
			Offset = float.Epsilon;
			CleanChildren(false);
			Recalculate();
		}

		void Update()
		{
			if (!EditorApplication.isPlaying)
			{
				CreateFrontAndBackIfNull();
				SetLocalPosition(frontImage, front, Offset);
				SetLocalPosition(backImage, back, -Offset);
				CleanChildren();
			}
		}
#endif

		void OnDestroy()
		{
			SafeDestroy(front);
			SafeDestroy(back);
		}

		void Start()
		{
#if UNITY_EDITOR
			if (EditorApplication.isPlaying)
			{
#endif
				front = null;
				back = null;
				CleanChildren(false);
				Recalculate();
#if UNITY_EDITOR
			}
#endif
		}




		#endregion



		/// <summary>
		/// Ensures that front and back aren't null and have been reset back to a default state with all their necessary components attached.
		/// </summary>
		private void ResetFrontAndBack()
		{
			if (front != null)
			{
				SafeDestroy(front);
			}
			front = CreateQuad("front", 180);
			if (back != null)
			{
				SafeDestroy(back);
			}
			back = CreateQuad("back", 0);
		}


		private void CreateFrontAndBackIfNull()
		{
			bool shouldRecalculate = false;
			if (front == null || back == null)
			{
				shouldRecalculate = true;
			}

			if (front == null)
			{
				front = CreateQuad("front", 180);
				Debug.LogWarning("GameObject 'front' cannot be deleted because it is part of a PlaneRenderer!");
			}
			if (back == null)
			{
				back = CreateQuad("back", 0);
				Debug.LogWarning("GameObject 'back' cannot be deleted because it is part of a PlaneRenderer!");
			}

			if (shouldRecalculate)
			{
				RecalculateExistingFrontAndBack();
			}

		}


		/// <summary>
		/// Creates a colliderless Quad with the given name as a child of this GameObject.
		/// </summary>
		private GameObject CreateQuad(string name, float rotation)
		{
			GameObject o = GameObject.CreatePrimitive(PrimitiveType.Quad);
			o.name = name;
			o.transform.SetParent(transform);
			SafeDestroy(o.GetComponent<MeshCollider>());
			o.GetComponent<MeshRenderer>().shadowCastingMode = castShadows;
			o.transform.localRotation = Quaternion.identity;
			o.transform.Rotate(new Vector3(0, rotation, 0));
			o.transform.localPosition = new Vector3(0, 0, 0);
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
		private void CleanChildren(bool showMessage = true)
		{
			foreach (Transform child in transform)
			{
				if (child != null &&
					(front == null || child != front?.transform) &&
					(back == null || child != back?.transform))
				{
					if (child != null && child.gameObject != null && !deletionQueue.Contains(child.gameObject))
					{
						if (showMessage)
						{
							Debug.LogWarning("GameObject '" + child.gameObject.name + "' was destroyed because it was added as a child of " +
								"a PlaneRenderer.\nIf an object has a PlaneRenderer component, its only children can be the 'front' and 'back' objects.");
#if UNITY_EDITOR
							Undo.DestroyObjectImmediate(child.gameObject);
#endif
						}
						if (child != null && child.gameObject != null)
						{
							SafeDestroy(child.gameObject);
						}
					}
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
		public void Recalculate()
		{
			ResetFrontAndBack();
			RecalculateExistingFrontAndBack();
			CleanChildren();
		}


		private void RecalculateExistingFrontAndBack()
		{
			if (front != null)
			{
				if (frontImage != null)
				{
					front.SetActive(true);
					RecalculateDimensions(frontImage, front, false, Offset);
				}
				else
				{
					front.SetActive(false);
				}
			}
			if (back != null)
			{
				if (backImage != null && back != null)
				{
					back.SetActive(true);
					RecalculateDimensions(backImage, back, mirrorBackImage, -Offset);
				}
				else
				{
					back.SetActive(false);
				}
			}
		}


		/// <summary>
		/// Recalculates the dimensions of the front and back quads, assuming they already exist.
		/// </summary>
		private void RecalculateDimensions(Texture2D image, GameObject plane, bool flip, float offset)
		{
			float width = image.width / pixelsPerMeter;
			float height = image.height / pixelsPerMeter;

			plane.transform.localScale = new Vector3(width * (flip ? -1 : 1), height, 1);

			SetTexture(plane, image);

			SetLocalPosition(image, plane, offset);
			plane.GetComponent<MeshRenderer>().shadowCastingMode = castShadows;

#if UNITY_EDITOR
			EditorUtility.SetDirty(plane);

			EditorUtility.SetDirty(this.gameObject);
#endif
		}


		private void SetLocalPosition(Texture2D image, GameObject plane, float offset)
		{
			if (image != null && plane != null)
			{
				float width = image.width / pixelsPerMeter;
				float height = image.height / pixelsPerMeter;
				plane.transform.localPosition = new Vector3(-width / 2 + anchorPoint.x * width, height / 2 - anchorPoint.y * height, offset);
			}
		}



		/// <summary>
		/// Calls appropriate destroy method depending on whether game is running or not.
		/// </summary>
		/// <param name="go"></param>
		private void SafeDestroy(UnityEngine.Object go)
		{
			RemoveNullObjects(deletionQueue);
			if (go == null)
			{
				return;
			}
#if UNITY_EDITOR
			if (!EditorApplication.isPlaying)
			{
				deletionQueue.Add(go);
				UnityEditor.EditorApplication.delayCall += () =>
				{
					deletionQueue.Remove(go);
					DestroyImmediate(go, false);
				};
			}
			else
			{
#endif
				deletionQueue.Add(go);
				Destroy(go);
#if UNITY_EDITOR
			}
#endif
		}


		private void RemoveNullObjects(HashSet<Object> set)
		{
			List<Object> toremove = new List<Object>();
			foreach (Object o in set)
			{
				if (!o)
				{
					toremove.Add(o);
				}
			}

			foreach (Object o in toremove)
			{
				set.Remove(o);
			}

		}

	}
}
