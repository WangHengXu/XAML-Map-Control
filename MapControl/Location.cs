﻿// XAML Map Control - http://xamlmapcontrol.codeplex.com/
// Copyright © Clemens Fischer 2012-2013
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Globalization;

namespace MapControl
{
    /// <summary>
    /// A geographic location with latitude and longitude values in degrees.
    /// </summary>
#if !SILVERLIGHT && !NETFX_CORE
    [Serializable]
#endif
    public partial class Location
    {
        /// <summary>
        /// TransformedLatitude is set by the Transform methods in MercatorTransform.
        /// It holds the transformed latitude value to avoid redundant recalculation. 
        /// </summary>
#if !SILVERLIGHT && !NETFX_CORE
        [NonSerialized]
#endif
        internal double TransformedLatitude;

        private double latitude;
        private double longitude;

        public Location()
        {
        }

        public Location(double lat, double lon)
        {
            Latitude = lat;
            Longitude = lon;
        }

        public double Latitude
        {
            get { return latitude; }
            set
            {
                latitude = Math.Min(Math.Max(value, -90d), 90d);
                TransformedLatitude = double.NaN;
            }
        }

        public double Longitude
        {
            get { return longitude; }
            set { longitude = value; }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:F5},{1:F5}", latitude, longitude);
        }

        public static Location Parse(string s)
        {
            var tokens = s.Split(new char[] { ',' });
            if (tokens.Length != 2)
            {
                throw new FormatException("Location string must be a comma-separated pair of double values");
            }

            return new Location(
                double.Parse(tokens[0], NumberStyles.Float, CultureInfo.InvariantCulture),
                double.Parse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture));
        }

        public static double NormalizeLongitude(double longitude)
        {
            return (longitude >= -180d && longitude <= 180d) ? longitude : ((longitude + 180d) % 360d + 360d) % 360d - 180d;
        }
    }
}
