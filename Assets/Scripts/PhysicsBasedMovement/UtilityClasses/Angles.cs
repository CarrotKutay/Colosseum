using Unity.Mathematics;

public static class Angles
{
    /// <summary>
    /// Get angle bewteen <paramref name="fromAxis"/> and <paramref name="toAxis"/> in radians. Uses atan2.
    /// </summary>
    public static float getAngleBetweenAxisWithAtan(float fromAxis, float toAxis)
    {
        return math.atan2(fromAxis, toAxis);
    }

    public static float3 getAxisAngles(float3 a_axis, float3 b_axis, bool withX = true, bool withY = true, bool withZ = true)
    {
        // * get x angle in radians
        var radiansAxisA_YtoZ = Angles.getAngleBetweenAxisWithAtan(a_axis.y, a_axis.z);
        var radiansAxisB_YtoZ = Angles.getAngleBetweenAxisWithAtan(b_axis.y, b_axis.z);
        var radiansXAxis = radiansAxisB_YtoZ - radiansAxisA_YtoZ;
        radiansXAxis = withX ? Angles.getSmallAngle(radiansXAxis) : 0;
        // * get y angle in radians
        var radiansAxisA_XtoZ = Angles.getAngleBetweenAxisWithAtan(a_axis.x, a_axis.z);
        var radiansAxisB_XtoZ = Angles.getAngleBetweenAxisWithAtan(b_axis.x, b_axis.z);
        var radiansYAxis = radiansAxisA_XtoZ - radiansAxisB_XtoZ;
        radiansYAxis = withY ? Angles.getSmallAngle(radiansYAxis) : 0;
        // * get z angle in radians
        var radiansAxisA_XtoY = Angles.getAngleBetweenAxisWithAtan(a_axis.x, a_axis.y);
        var radiansAxisB_XtoY = Angles.getAngleBetweenAxisWithAtan(b_axis.x, b_axis.y);
        var radiansZAxis = radiansAxisA_XtoY - radiansAxisB_XtoY;
        radiansZAxis = withZ ? Angles.getSmallAngle(radiansZAxis) : 0;

        return new float3(radiansXAxis, radiansYAxis, radiansZAxis);
    }

    /// <summary>
    /// Get angle bewteen <paramref name="fromAxis"/> and <paramref name="toAxis"/> in radians. Uses dot product see
    /// https://onlinemschool.com/math/library/vector/angl/ .
    /// Returns angle in degrees.
    /// </summary>
    public static float getAngleBetweeVectorsWithDot(float3 fromAxis, float3 toAxis)
    {
        return math.acos(
            math.dot(fromAxis, toAxis) / (math.length(fromAxis) * math.length(toAxis))
        );
    }

    /// <summary>
    /// Alter the given angle (<paramref name="angleInRadians"/>) to always be below 180 degrees.
    /// <para>Returns the angle in radians the angle will be (translated to degrees) between 0 to 180 or -(0 to 180) degrees</para>
    /// </summary>
    public static float getSmallAngle(float angleInRadians)
    {
        /* 
            * Determine the difference between the angle value of the surfaceNormal and
            * the entity upward vector. Furthermore, we alter the difference to always use the
            * small difference, since we want to apply control force efficiently and as fast as possible
        */
        if (math.abs(angleInRadians) > math.PI)
        {
            angleInRadians = angleInRadians < 0 ?
                 angleInRadians + math.PI * 2 :
                 (math.PI * 2 - angleInRadians) * -1;
        }
        return angleInRadians;
    }

    public static float regulateVelocityStrength(float angleInRadians, float angularVelocityStrengthRegulation = 1f)
    {
        /* 
            * Changing the velocity change strength depending on how big the angle is.
            * Using a square root function to determine velocity strength and multiply it by angularVelocityStrength
            * to further increase velocity the larger the angle.
        */

        return angleInRadians > 0 ?
                math.pow(math.abs(angleInRadians) / math.PI, .5f) * angularVelocityStrengthRegulation :
                math.pow(math.abs(angleInRadians) / math.PI, .5f) * angularVelocityStrengthRegulation * -1;
    }
}
