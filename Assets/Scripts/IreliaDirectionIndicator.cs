using UnityEngine;
using UnityEngine.Rendering;

namespace MidLane
{
    // Shared presentation for held R and charging W. Never participates in combat physics.
    public sealed class IreliaDirectionIndicator : MonoBehaviour
    {
        Transform trail, tip;
        LineRenderer outline;
        Material trailMaterial, tipMaterial, outlineMaterial;
        Mesh ground;
        public float Range { get; private set; }
        public float Width { get; private set; }
        public Vector3 Direction { get; private set; }
        public static IreliaDirectionIndicator Create(Transform parent, string label)
        {
            var root=new GameObject(label); root.transform.SetParent(parent,false);
            var view=root.AddComponent<IreliaDirectionIndicator>(); view.Build(); return view;
        }
        public void Present(Vector3 origin, Vector3 direction, float range, float width)
        {
            Range=range; Width=width; direction.y=0;
            Direction=direction.sqrMagnitude>.0001f?direction.normalized:Vector3.forward;
            transform.SetPositionAndRotation(origin+Vector3.up*.055f,Quaternion.LookRotation(Direction));
            float half=width*.5f, head=Mathf.Min(.9f,range*.15f);
            outline.SetPositions(new[] {new Vector3(-half,0,0),new Vector3(-half,0,range-head),
                new Vector3(0,0,range),new Vector3(half,0,range-head),new Vector3(half,0,0)});
            trail.localScale=new Vector3(width,1,range);
            tip.localPosition=new Vector3(0,.006f,range-head*1.2f);
            tip.localScale=new Vector3(width,1,head*1.2f);
            gameObject.SetActive(true);
        }
        public void Hide() { gameObject.SetActive(false); }
        void Build()
        {
            ground=new Mesh {name="Directional aiming ground quad"};
            ground.vertices=new[] {new Vector3(-.5f,0,0),new Vector3(.5f,0,0),new Vector3(-.5f,0,1),new Vector3(.5f,0,1)};
            ground.uv=new[] {Vector2.zero,Vector2.right,Vector2.up,Vector2.one};
            ground.triangles=new[] {0,2,1,1,2,3}; ground.RecalculateBounds();
            trailMaterial=new Material(Shader.Find("MidLane/IreliaRAim"));
            trailMaterial.SetColor("_Tint",new Color(.25f,.82f,1,1));
            trailMaterial.SetTexture("_BaseMap",Resources.Load<Texture2D>(IreliaGlbAssets.Root+"R/AimTrail"));
            trail=Surface("Official R direction texture",trailMaterial);
            tipMaterial=new Material(trailMaterial);
            tipMaterial.SetTexture("_BaseMap",Resources.Load<Texture2D>(IreliaGlbAssets.Root+"R/AimTip"));
            tip=Surface("Official R tip texture",tipMaterial);
            outlineMaterial=new Material(trailMaterial); outlineMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);
            outlineMaterial.SetColor("_Tint",new Color(.15f,.8f,1,.85f));
            outline=gameObject.AddComponent<LineRenderer>(); outline.useWorldSpace=false; outline.loop=true;
            outline.positionCount=5; outline.startWidth=outline.endWidth=.045f;
            outline.sharedMaterial=outlineMaterial; outline.shadowCastingMode=ShadowCastingMode.Off; outline.receiveShadows=false;
        }
        Transform Surface(string label, Material material)
        {
            var obj=new GameObject(label); obj.transform.SetParent(transform,false);
            obj.AddComponent<MeshFilter>().sharedMesh=ground;
            var renderer=obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false; return obj.transform;
        }
        void OnDestroy()
        {
            if (ground!=null) Destroy(ground);
            if (trailMaterial!=null) Destroy(trailMaterial);
            if (tipMaterial!=null) Destroy(tipMaterial);
            if (outlineMaterial!=null) Destroy(outlineMaterial);
        }
    }
}
