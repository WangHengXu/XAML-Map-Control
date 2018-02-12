﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2018 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace MapControl
{
    /// <summary>
    /// A polyline defined by a collection of Locations.
    /// </summary>
    public class MapPolyline : MapShape
    {
        public static readonly DependencyProperty LocationsProperty = DependencyProperty.Register(
            nameof(Locations), typeof(IEnumerable<Location>), typeof(MapPolyline),
            new PropertyMetadata(null, (o, e) => ((MapPolyline)o).LocationsPropertyChanged(e)));

        /// <summary>
        /// Gets or sets the Locations that define the polyline points.
        /// </summary>
        public IEnumerable<Location> Locations
        {
            get { return (IEnumerable<Location>)GetValue(LocationsProperty); }
            set { SetValue(LocationsProperty, value); }
        }

        protected override void UpdateData()
        {
            ((PathGeometry)Data).Figures.Clear();

            if (ParentMap != null && Locations != null && Locations.Count() >= 2)
            {
                var locations = Locations;
                var offset = GetLongitudeOffset();

                if (offset != 0d)
                {
                    locations = locations.Select(loc => new Location(loc.Latitude, loc.Longitude + offset));
                }

                var points = locations.Select(loc => ParentMap.MapProjection.LocationToViewportPoint(loc)).ToList();

                CreatePolylineFigures(points);
            }
        }
    }
}
