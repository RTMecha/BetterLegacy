using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace BetterLegacy.Core.Data
{
    // this class demonstrates how inverse kinematics works. based on https://github.com/eddyariki/UnityInverseKinematics

    public class InverseKinematics : Exists
    {
        public InverseKinematics() { }

        public Bone[] bones;

        public Vector3 baseTarget;
        public Vector3 target;
        public bool calculateForward = true;
        public bool depthSpace = false;

        public void InitBones(Bone[] bones, Vector3 baseTarget, Vector2 target)
        {
            this.bones = bones;
            this.baseTarget = baseTarget;
            this.target = target;
            UpdateIK();
        }

        public void Set(Vector3 baseTarget, Vector3 target)
        {
            this.baseTarget = baseTarget;
            this.target = target;
            UpdateIK();
        }

        /// <summary>
        /// Updates the kinematics.
        /// </summary>
        public void UpdateIK()
        {
            for (int i = 0; i < bones.Length; i++)
                bones[i].Reset();
            CalculateIK();
            if (calculateForward)
                CalculateFK();
            for (int i = 0; i < bones.Length; i++)
                bones[i].Apply();
        }

        // today I learned that inverse kinematics is called that way because it goes backwards through the parent chain, instead of just forwards

        Quaternion FlatRotation(Vector2 pos) => Quaternion.Euler(0f, 0f, Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg);

        /// <summary>
        /// Calculates inverse kinematics.
        /// </summary>
        public void CalculateIK()
        {
            for (int i = bones.Length - 1; i >= 0; i--)
            {
                var bone = bones[i];
                if (i == bones.Length - 1)
                {
                    bone.position = target - Vector3.Normalize(target - bone.position) * bone.length / 10f;
                    bone.rotation = !depthSpace ? FlatRotation(target - bone.position) : Quaternion.LookRotation(target - bone.position, Vector3.up);
                }
                else
                {
                    bone.position = bones[i + 1].position - Vector3.Normalize(bones[i + 1].position - bone.position) * bone.length;
                    bone.rotation = !depthSpace ? FlatRotation(bones[i + 1].position - bone.position) : Quaternion.LookRotation(bones[i + 1].position - bone.position, Vector3.up);
                }
            }
        }

        /// <summary>
        /// Calculates forward kinematics.
        /// </summary>
        public void CalculateFK()
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                if (i == 0)
                {
                    bone.position = baseTarget;
                    bone.rotation = !depthSpace ? FlatRotation(bones[i + 1].position - bone.position) : Quaternion.LookRotation(bones[i + 1].position - bone.position, Vector3.up);
                }
                else
                {
                    bone.position = bones[i - 1].position - Vector3.Normalize(bones[i - 1].position - bone.position) * bone.length;
                    bone.rotation = !depthSpace ? FlatRotation(bones[i - 1].position - bone.position) : Quaternion.LookRotation(bones[i - 1].position - bone.position, Vector3.up);
                }
            }
        }

        public static InverseKinematics Test()
        {
            var mesh = GameManager.inst.PlayerPrefabs[1].GetComponentInChildren<MeshFilter>().mesh;
            var top = Creator.NewGameObject("test", null);
            var ikTest = top.AddComponent<IKTest>();
            var obj1 = ObjectManager.inst.objectPrefabs[0].options[0].Duplicate(top.transform, "obj1");
            obj1.SetActive(true);
            obj1.transform.localScale = Vector3.one;
            obj1.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.white;
            obj1.transform.GetChild(0).GetComponent<MeshFilter>().mesh = mesh;
            var obj2 = ObjectManager.inst.objectPrefabs[0].options[0].Duplicate(top.transform, "obj2");
            obj2.SetActive(true);
            obj2.transform.localScale = Vector3.one;
            obj2.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.white;
            obj2.transform.GetChild(0).GetComponent<MeshFilter>().mesh = mesh;
            var obj3 = ObjectManager.inst.objectPrefabs[0].options[0].Duplicate(top.transform, "obj3");
            obj3.SetActive(true);
            obj3.transform.localScale = Vector3.one;
            obj3.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.white;
            obj3.transform.GetChild(0).GetComponent<MeshFilter>().mesh = mesh;

            var ik = new InverseKinematics();
            ikTest.ik = ik;
            ik.InitBones(new Bone[]
            {
                new TransformBone(obj1.transform),
                new TransformBone(obj2.transform),
                new TransformBone(obj3.transform),
            }, Vector3.zero, new Vector3(10f, 5f));
            return ik;
        }

        public class IKTest : MonoBehaviour
        {
            public InverseKinematics ik;

            public Func<Vector2> getBasePos;

            public Func<Vector2> getPos;

            void Update()
            {
                if (!ik)
                    return;
                if (getBasePos != null)
                    ik.baseTarget = getBasePos.Invoke();
                if (getPos != null)
                    ik.target = getPos.Invoke();
                ik.UpdateIK();
            }
        }

        public class Bone : Exists
        {
            public Vector3 position;
            public Quaternion rotation;
            public float length = 5f;
            public void Reset()
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
            }
            public virtual void Apply() { }
        }

        public class TransformBone : Bone
        {
            public TransformBone(Transform transform) => this.transform = transform;

            public Transform transform;

            public bool applyPosition = true;

            public override void Apply()
            {
                if (applyPosition)
                    transform.position = position;
                transform.rotation = rotation;
            }
        }
    }
}
