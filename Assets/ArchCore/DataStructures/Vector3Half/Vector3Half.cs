using System;
using UnityEngine;

namespace ArchCore.DataStructures.Vector3Half
{
    public readonly struct Vector3Half
    {
        public readonly Half x;
        public readonly Half y;
        public readonly Half z;

        public Vector3Half(Vector3 vector)
        {
            x = (Half)vector.x;
            y = (Half)vector.y;
            z = (Half)vector.z;
        }

        public Vector3Half(Half x, Half y, Half z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static explicit operator Vector3Half(Vector3 value) => new Vector3Half(value);

        public static implicit operator Vector3(Vector3Half value) => new Vector3(value.x, value.y, value.z);
    }
}