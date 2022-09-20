using System.Runtime.CompilerServices;
using Unity.Mathematics;

using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;

namespace RTTCamera
{
    internal static class CameraUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float clampAngle(float lfAngle, float lfMin, float lfMax)
        {
            lfAngle += select(0, 360f, lfAngle < -180f);
            lfAngle -= select(0, 360f, lfAngle > 180f);
            return clamp(lfAngle, lfMin, lfMax);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion RotateFWorld(in quaternion localRotation, float x, float y, float z)
        {
            quaternion eulerRot = quaternion.EulerZXY(x, y, z);
            return mul(eulerRot, localRotation);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion RotateFSelf(in quaternion localRotation, float x, float y, float z)
        {
            quaternion eulerRot = quaternion.EulerZXY(x, y, z);
            return mul(localRotation, eulerRot);
        }
/*
        public static float3 UnityQuaternionToEuler(this quaternion q2)
        {
            float4 q1 = q2.value;
 
            float sqw = q1.w * q1.w;
            float sqx = q1.x * q1.x;
            float sqy = q1.y * q1.y;
            float sqz = q1.z * q1.z;
            float unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            float test = q1.x * q1.w - q1.y * q1.z;
            float3 v;
            
            if (test > 0.4995f * unit)
            { // singularity at north pole
                v.y = 2f * atan2(q1.y, q1.x);
                v.x = PI / 2f;
                v.z = 0;
                return NormalizeAngles(degrees(v));
            }
            if (test < -0.4995f * unit)
            { // singularity at south pole
                v.y = -2f * atan2(q1.y, q1.x);
                v.x = -PI / 2f;
                v.z = 0;
                return NormalizeAngles(degrees(v));
            }
 
            quaternion q3 = new quaternion(q1.w, q1.z, q1.x, q1.y);
            float4 q = q3.value;
 
            v.y = atan2(2f * q.x * q.w + 2f * q.y * q.z, 1 - 2f * (q.z * q.z + q.w * q.w));   // Yaw
            v.x = asin(2f * (q.x * q.z - q.w * q.y));                                         // Pitch
            v.z = atan2(2f * q.x * q.y + 2f * q.z * q.w, 1 - 2f * (q.y * q.y + q.z * q.z));   // Roll
 
            return NormalizeAngles(degrees(v));
        }
        
        private static float3 NormalizeAngles(float3 angles)
        {
            angles.x = NormalizeAngle(angles.x);
            angles.y = NormalizeAngle(angles.y);
            angles.z = NormalizeAngle(angles.z);
            return angles;
        }
 
        private static float NormalizeAngle(float angle)
        {
            //Debug.Log($"angle: {angle} radian 360 = {radians(360)}");
            //angle -= select(0, 360, angle > 360);
            //angle += select(0, 360, angle < 0);
            
            while (angle > 360)
                angle -= 360;
            while (angle < 0)
                angle += 360;
                
            return angle;
        }
        */
        /*
        public static quaternion UnityEulerToQuaternion(float3 v)
        {
            return UnityEulerToQuaternion(v.y, v.x, v.z);
        }
 
        public static quaternion UnityEulerToQuaternion(float yaw, float pitch, float roll)
        {
            yaw = radians(yaw);
            pitch = radians(pitch);
            roll = radians(roll);
 
            float rollOver2 = roll * 0.5f;
            float sinRollOver2 = (float)sin((double)rollOver2);
            float cosRollOver2 = (float)cos((double)rollOver2);
            float pitchOver2 = pitch * 0.5f;
            float sinPitchOver2 = (float)sin((double)pitchOver2);
            float cosPitchOver2 = (float)cos((double)pitchOver2);
            float yawOver2 = yaw * 0.5f;
            float sinYawOver2 = (float)sin((double)yawOver2);
            float cosYawOver2 = (float)cos((double)yawOver2);
            float4 result;
            result.w = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2;
            result.x = cosYawOver2 * sinPitchOver2 * cosRollOver2 + sinYawOver2 * cosPitchOver2 * sinRollOver2;
            result.y = sinYawOver2 * cosPitchOver2 * cosRollOver2 - cosYawOver2 * sinPitchOver2 * sinRollOver2;
            result.z = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2;
 
            return new quaternion(result);
        }
        */
    
        //==============================================================================================================
        //FROM UNITY PHYSICS
        
        /// <summary>
        /// Convert a quaternion orientation to Euler angles.
        /// Use this method to calculate angular velocity needed to achieve a target orientation.
        /// </summary>
        /// <param name="q">An orientation.</param>
        /// <param name="order"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToEulerAngles(this quaternion q, RotationOrder order = RotationOrder.XYZ)
        {
            return toEuler(q, order);
        }
        
        // Note: taken from Unity.Animation/Core/MathExtensions.cs, which will be moved to Unity.Mathematics at some point
        //       after that, this should be removed and the Mathematics version should be used
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 toEuler(in quaternion q, RotationOrder order = RotationOrder.Default)
        {
            const float epsilon = 1e-6f;
 
            //prepare the data
            float4 FOUR = new float4(2.0f);
            float4 qv = q.value;
            float4 d1 = qv * qv.wwww * FOUR; //xw, yw, zw, ww
            float4 d2 = qv * qv.yzxw * FOUR; //xy, yz, zx, ww
            float4 d3 = qv * qv;
            float3 euler = float3.zero;
            
            const float CUTOFF = (1.0f - 2.0f * epsilon) * (1.0f - 2.0f * epsilon);
            
            switch (order)
            {
                case RotationOrder.ZYX:
                {
                    float y1 = d2.z + d1.y;
                    
                    if (y1 * y1 < CUTOFF)
                    {
                        float x1 = -d2.x + d1.z;
                        float x2 = d3.x + d3.w - d3.y - d3.z;
                        float z1 = -d2.y + d1.x;
                        float z2 = d3.z + d3.w - d3.y - d3.x;
                        euler = new float3(atan2(x1, x2), asin(y1), atan2(z1, z2));
                    }
                    else //zxz
                    {
                        y1 = clamp(y1, -1.0f, 1.0f);
                        float4 abcd = new float4(d2.z, d1.y, d2.y, d1.x);
                        float x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        float x2 = csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(atan2(x1, x2), asin(y1), 0.0f);
                    }
                    break;
                }
 
                case RotationOrder.ZXY:
                {
                    float y1 = d2.y - d1.x;
                    if (y1 * y1 < CUTOFF)
                    {
                        float x1 = d2.x + d1.z;
                        float x2 = d3.y + d3.w - d3.x - d3.z;
                        float z1 = d2.z + d1.y;
                        float z2 = d3.z + d3.w - d3.x - d3.y;
                        euler = new float3(atan2(x1, x2), -asin(y1), atan2(z1, z2));
                    }
                    else //zxz
                    {
                        y1 = clamp(y1, -1.0f, 1.0f);
                        float4 abcd = new float4(d2.z, d1.y, d2.y, d1.x);
                        float x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        float x2 = csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(atan2(x1, x2), -asin(y1), 0.0f);
                    }
                    break;
                }
 
                case RotationOrder.YXZ:
                {
                    float y1 = d2.y + d1.x;
                    if (y1 * y1 < CUTOFF)
                    {
                        float x1 = -d2.z + d1.y;
                        float x2 = d3.z + d3.w - d3.x - d3.y;
                        float z1 = -d2.x + d1.z;
                        float z2 = d3.y + d3.w - d3.z - d3.x;
                        euler = new float3(atan2(x1, x2), asin(y1), atan2(z1, z2));
                    }
                    else //yzy
                    {
                        y1 = clamp(y1, -1.0f, 1.0f);
                        float4 abcd = new float4(d2.x, d1.z, d2.y, d1.x);
                        float x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        float x2 = csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(atan2(x1, x2), asin(y1), 0.0f);
                    }
                    break;
                }
 
                case RotationOrder.YZX:
                {
                    float y1 = d2.x - d1.z;
                    if (y1 * y1 < CUTOFF)
                    {
                        float x1 = d2.z + d1.y;
                        float x2 = d3.x + d3.w - d3.z - d3.y;
                        float z1 = d2.y + d1.x;
                        float z2 = d3.y + d3.w - d3.x - d3.z;
                        euler = new float3(atan2(x1, x2), -asin(y1), atan2(z1, z2));
                    }
                    else //yxy
                    {
                        y1 = clamp(y1, -1.0f, 1.0f);
                        float4 abcd = new float4(d2.x, d1.z, d2.y, d1.x);
                        float x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        float x2 = csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(atan2(x1, x2), -asin(y1), 0.0f);
                    }
                    break;
                }
 
                case RotationOrder.XZY:
                {
                    float y1 = d2.x + d1.z;
                    if (y1 * y1 < CUTOFF)
                    {
                        float x1 = -d2.y + d1.x;
                        float x2 = d3.y + d3.w - d3.z - d3.x;
                        float z1 = -d2.z + d1.y;
                        float z2 = d3.x + d3.w - d3.y - d3.z;
                        euler = new float3(atan2(x1, x2), asin(y1), atan2(z1, z2));
                    }
                    else //xyx
                    {
                        y1 = clamp(y1, -1.0f, 1.0f);
                        float4 abcd = new float4(d2.x, d1.z, d2.z, d1.y);
                        float x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        float x2 = csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(atan2(x1, x2), asin(y1), 0.0f);
                    }
                    break;
                }
 
                case RotationOrder.XYZ:
                {
                    float y1 = d2.z - d1.y;
                    if (y1 * y1 < CUTOFF)
                    {
                        float x1 = d2.y + d1.x;
                        float x2 = d3.z + d3.w - d3.y - d3.x;
                        float z1 = d2.x + d1.z;
                        float z2 = d3.x + d3.w - d3.y - d3.z;
                        euler = new float3(atan2(x1, x2), -asin(y1), atan2(z1, z2));
                    } else //xzx
                    {
                        y1 = clamp(y1, -1.0f, 1.0f);
                        float4 abcd = new float4(d2.z, d1.y, d2.x, d1.z);
                        float x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        float x2 = csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(atan2(x1, x2), -asin(y1), 0.0f);
                    }
                    break;
                }
            }
 
            return EulerReorderBack(euler, order);
        }
 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 EulerReorderBack(in float3 euler, RotationOrder order) =>
        order switch
        {
            RotationOrder.XZY => euler.xzy,
            RotationOrder.YZX => euler.zxy,
            RotationOrder.YXZ => euler.yxz,
            RotationOrder.ZXY => euler.yzx,
            RotationOrder.ZYX => euler.zyx,
            RotationOrder.XYZ => euler,
            _ => euler
        };
        
        /*
        private static float3 eulerReorderBack(float3 euler, RotationOrder order)
        {
            switch (order)
            {
                case math.RotationOrder.XZY:
                    return euler.xzy;
                case math.RotationOrder.YZX:
                    return euler.zxy;
                case math.RotationOrder.YXZ:
                    return euler.yxz;
                case math.RotationOrder.ZXY:
                    return euler.yzx;
                case math.RotationOrder.ZYX:
                    return euler.zyx;
                case math.RotationOrder.XYZ:
                default:
                    return euler;
            }
        }
        */
        
        
    }
}