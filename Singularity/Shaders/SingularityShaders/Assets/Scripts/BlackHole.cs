using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BlackHole : MonoBehaviour
{
    
	[SerializeField]
	Material blackHoleMaterial;

	[SerializeField]
	Texture2D accretionDisk;

	[SerializeField]
	Texture2D radialAccretionDisk;

	[SerializeField]
	bool useAccretionDisk;

	[SerializeField]
	bool radialMapping;

	[SerializeField]
	float radius;

	[SerializeField]
	float gravity;

	[SerializeField]
	Camera sceneCam;

	[SerializeField]
	Vector3 DiskNormal;

	[SerializeField]
	float DiskInnerRadius;

	[SerializeField]
	float DiskOuterRadius;

	[SerializeField]
	float rotationSpeed;

	RenderTexture screenBuffer;
	CommandBuffer screenCopyCommandBuffer;

	Vector3 initialCamPos;


//	public float xRot = 0f;
//	public float yRot = 0f;

	public float sensitivity = 1000f;

    void Start()
    {				
		initialCamPos = sceneCam.transform.position;
		blackHoleMaterial.SetColor("galaxyFadeColor",Color.white);
		blackHoleMaterial.SetMatrix("cubeMapRotation",Matrix4x4.identity);

		screenBuffer = new RenderTexture (Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, 0);
		screenBuffer.Create ();

		screenCopyCommandBuffer = new CommandBuffer();
		screenCopyCommandBuffer.name = "SingularityGrabScreen";
		screenCopyCommandBuffer.Blit (BuiltinRenderTextureType.CurrentActive, screenBuffer);
		sceneCam.AddCommandBuffer (CameraEvent.AfterForwardOpaque, screenCopyCommandBuffer);
		blackHoleMaterial.SetTexture("screenBuffer",screenBuffer);
    }
		
    void Update()
    {
		//blackHoleMaterial.SetVector("blackhole", new Vector4(gameObject.transform.position.x,gameObject.transform.position.y,gameObject.transform.position.z,radius));
		blackHoleMaterial.SetFloat("blackHoleRadius",radius);
		blackHoleMaterial.SetFloat("gravity", gravity);
		blackHoleMaterial.SetVector("blackholeDisk", new Vector4(DiskInnerRadius*DiskNormal.x,DiskInnerRadius*DiskNormal.y,DiskInnerRadius*DiskNormal.z,DiskOuterRadius));
		blackHoleMaterial.SetVector("diskNormal", DiskNormal);
		blackHoleMaterial.SetFloat("diskInnerRadius", DiskInnerRadius);
		blackHoleMaterial.SetFloat("diskOuterRadius", DiskOuterRadius);

		//gameObject.transform.position = new Vector3(Mathf.Sin(0.53f*Time.time), 2f+3f*Mathf.Cos(0.74f*Time.time), Mathf.Cos(0.22f*Time.time));

		sceneCam.transform.RotateAround(gameObject.transform.position, Vector3.up, 10 * Time.deltaTime);
		float initialLength = (initialCamPos - gameObject.transform.position).magnitude;
		//sceneCam.transform.position = gameObject.transform.position + (sceneCam.transform.position - gameObject.transform.position).normalized * Mathf.Lerp(initialLength*0.3f, initialLength, 0.5f * (1.0f + Mathf.Cos(0.4f*Time.time)));
		sceneCam.transform.position = gameObject.transform.position + (sceneCam.transform.position - gameObject.transform.position).normalized * Mathf.Lerp(initialLength*0.2f, initialLength*1.5f, 0.5f * (1.0f + Mathf.Cos(0.4f*Time.time)));

		if (radialMapping)
		{
			blackHoleMaterial.DisableKeyword ("RADIAL_DISK_MAPPING_OFF");
			blackHoleMaterial.EnableKeyword ("RADIAL_DISK_MAPPING_ON");
			blackHoleMaterial.SetTexture("AccretionDisk", radialAccretionDisk);
		}
		else
		{
			blackHoleMaterial.DisableKeyword ("RADIAL_DISK_MAPPING_ON");
			blackHoleMaterial.EnableKeyword ("RADIAL_DISK_MAPPING_OFF");
			blackHoleMaterial.SetTexture("AccretionDisk", accretionDisk);
		}

		if (useAccretionDisk)
		{
			blackHoleMaterial.DisableKeyword ("ACCRETION_DISK_OFF");
			blackHoleMaterial.EnableKeyword ("ACCRETION_DISK_ON");
		}
		else
		{
			blackHoleMaterial.DisableKeyword ("ACCRETION_DISK_ON");
			blackHoleMaterial.EnableKeyword ("ACCRETION_DISK_OFF");
		}

		blackHoleMaterial.DisableKeyword ("GALAXYCUBEMAPONLY_OFF");
		blackHoleMaterial.EnableKeyword ("GALAXYCUBEMAPONLY_ON");

		blackHoleMaterial.DisableKeyword ("WORMHOLE_ON");
		blackHoleMaterial.EnableKeyword ("WORMHOLE_OFF");

		blackHoleMaterial.SetFloat("universalTime", Time.time);
		blackHoleMaterial.SetFloat("rotationSpeed", rotationSpeed * (Mathf.PI * 2f) / 60f);


		//sceneCam.transform.LookAt(gameObject.transform);
		//sceneCam.transform.position = Vector3.Lerp(sceneCam.transform.position,gameObject.transform.position,0.2f*Mathf.Cos(0.4f*Time.time));
		//Debug.Log("blackhole: "+new Vector4(gameObject.transform.position.x,gameObject.transform.position.y,gameObject.transform.position.z,gameObject.transform.localScale.x).ToString());

//		float initialLength = (initialCamPos - gameObject.transform.position).magnitude;
//
//		xRot += Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
//		yRot += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
//
//		if(xRot > 90f)
//		{
//			xRot = 90f;
//		}
//		else if(xRot < -90f)
//		{
//			xRot = -90f;
//		}
//
//		sceneCam.transform.position = gameObject.transform.position + Quaternion.Euler(xRot, yRot, 0f) * (initialLength * -Vector3.back);
//		sceneCam.transform.LookAt(gameObject.transform.position, Vector3.up);
    }

	void OnDestroy()
	{
		if (!ReferenceEquals(sceneCam,null))
			sceneCam.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque,screenCopyCommandBuffer);
		screenBuffer.Release ();
	}
}
