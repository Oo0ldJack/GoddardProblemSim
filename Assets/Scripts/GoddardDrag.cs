using System;
using Goddard.Core;
using UnityEngine;

namespace Goddard.Stand
{
    /// <summary>
    /// Сопротивление считается формулой ЯДРА (Drag.D) — стенд и модель
    /// обязаны использовать одну формулу. Не путать с Rigidbody.linearDamping.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GoddardDrag : MonoBehaviour
    {
        public float LastDragModel { get; private set; }

        private Rigidbody rb;
        void Awake() => rb = GetComponent<Rigidbody>();

        void FixedUpdate()
        {
            // Модель атмосферы определена для h >= H0; ниже старта держим плотность «у земли»,
            // иначе exp(...) переполняется, и AddForce получает Infinity/NaN.
            double h = Math.Max(SceneUnits.ToModelHeight(transform.position.y), Params.P.H0);
            double v = SceneUnits.ToModelVelocity(rb.linearVelocity.y);

            double d = Drag.D(h, v, Params.P);   // формула ядра!
            LastDragModel = (float)d;

            // сила всегда против скорости
            float fUnity = SceneUnits.ToUnityForce(d) * Mathf.Sign(rb.linearVelocity.y);
            rb.AddForce(Vector3.down * fUnity);
        }
    }
}