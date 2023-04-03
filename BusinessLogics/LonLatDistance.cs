using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderApiFun.BusinessLogics
{
    public enum DistanceUnit
    {
        Kilometers,
        Miles
    }

    /// <summary>
    /// 地球经纬度距离计算器,
    /// 仅支持直线距离
    /// </summary>
    public static class LonLatDistance
    {
        /// <summary>
        /// 直线距离计算, Haversine公式
        /// </summary>
        /// <param name="startLat"></param>
        /// <param name="startLon"></param>
        /// <param name="endLat"></param>
        /// <param name="endLon"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static double CalculateDistance(double startLat, double startLon, double endLat, double endLon, DistanceUnit unit = DistanceUnit.Kilometers)
        {
            var dLat = (endLat - startLat) * Math.PI / 180.0;
            var dLon = (endLon - startLon) * Math.PI / 180.0;
            var lat1 = startLat * Math.PI / 180.0;
            var lat2 = endLat * Math.PI / 180.0;

            var a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Pow(Math.Sin(dLon / 2), 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = 6371 * c; // Radius of Earth in kilometers

            if (unit == DistanceUnit.Miles) distance /= 1.609344;
            return distance;
        }
    }
}
