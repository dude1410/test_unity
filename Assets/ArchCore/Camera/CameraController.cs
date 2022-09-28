using UnityEngine;

namespace ArchCore.CameraControl
{
	[RequireComponent(typeof(Camera))]
	public class CameraController : MonoBehaviour
	{
		[SerializeField] private new Camera camera;

		public Camera CurrentCamera => camera;

		public Vector3 Position
		{
			get => camera.transform.position;
			set => camera.transform.position = value;
		}
		
		public float FOV
		{
			get => camera.fieldOfView;
			set => camera.fieldOfView = value;
		}

		//....
		public void SetCameraToPosition(Vector3 pos)
		{
			camera.transform.position = pos;
		}
		
		public void SetEulerAngles(Vector3 ea)
		{
			camera.transform.eulerAngles = ea;
		}

		public Vector3 CameraEulerAngles => camera.transform.eulerAngles;

		public void SetActive(bool b)
		{
			if(camera.enabled != b)
				camera.enabled = b;
		}

	}
}
