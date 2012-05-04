﻿// WPF MapControl - http://wpfmapcontrol.codeplex.com/
// Copyright © 2012 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections;

namespace MapControl
{
    internal class TileContainer : ContainerVisual
    {
        private const double maxScaledTileSize = 400d; // scaled tile size 200..400 units
        private static double zoomLevelSwitchOffset = Math.Log(maxScaledTileSize / 256d, 2d);

        private Size size;
        private Point origin;
        private Vector offset;
        private double rotation;
        private double zoomLevel;
        private int tileZoomLevel;
        private Int32Rect tileGrid;
        private TileLayerCollection tileLayers;
        private readonly DispatcherTimer updateTimer;
        private readonly MatrixTransform viewportTransform = new MatrixTransform();

        public TileContainer()
        {
            updateTimer = new DispatcherTimer(TimeSpan.FromSeconds(0.5), DispatcherPriority.Background, UpdateTiles, Dispatcher);
        }

        public TileLayerCollection TileLayers
        {
            get { return tileLayers; }
            set
            {
                if (tileLayers != null)
                {
                    tileLayers.CollectionChanged -= TileLayersChanged;
                }

                tileLayers = value;
                ClearChildren();

                if (tileLayers != null)
                {
                    tileLayers.CollectionChanged += TileLayersChanged;
                    AddChildren(0, tileLayers);
                }

                ((Map)VisualParent).OnTileLayersChanged();
            }
        }

        public Transform ViewportTransform
        {
            get { return viewportTransform; }
        }

        public double SetTransform(double mapZoomLevel, double mapRotation, Point mapOrigin, Point viewportOrigin, Size viewportSize)
        {
            zoomLevel = mapZoomLevel;
            rotation = mapRotation;
            size = viewportSize;
            origin = viewportOrigin;

            double scale = Math.Pow(2d, zoomLevel) * 256d / 360d;
            offset.X = origin.X - (180d + mapOrigin.X) * scale;
            offset.Y = origin.Y - (180d - mapOrigin.Y) * scale;

            Matrix transform = new Matrix(1d, 0d, 0d, -1d, 180d, 180d);
            transform.Scale(scale, scale);
            transform.Translate(offset.X, offset.Y);
            transform.RotateAt(rotation, origin.X, origin.Y);
            viewportTransform.Matrix = transform;

            transform = GetVisualTransform();

            if (tileLayers != null)
            {
                foreach (TileLayer tileLayer in tileLayers)
                {
                    tileLayer.TransformMatrix = transform;
                }
            }

            updateTimer.IsEnabled = true;

            return scale;
        }

        private Matrix GetVisualTransform()
        {
            // Calculates the transform matrix that enables rendering of 256x256 tile rectangles in
            // TileLayer.UpdateTiles with origin at tileGrid.X and tileGrid.Y to minimize rounding errors.

            double scale = Math.Pow(2d, zoomLevel - tileZoomLevel);
            Matrix transform = new Matrix(1d, 0d, 0d, 1d, tileGrid.X * 256d, tileGrid.Y * 256d);
            transform.Scale(scale, scale);
            transform.Translate(offset.X, offset.Y);
            transform.RotateAt(rotation, origin.X, origin.Y);

            return transform;
        }

        private void UpdateTiles(object sender, EventArgs eventArgs)
        {
            updateTimer.IsEnabled = false;

            int zoom = (int)Math.Floor(zoomLevel + 1d - zoomLevelSwitchOffset);
            int numTiles = 1 << zoom;
            double mapToTileScale = (double)numTiles / 360d;
            Matrix transform = viewportTransform.Matrix;
            transform.Invert(); // view to map coordinates
            transform.Translate(180d, -180d);
            transform.Scale(mapToTileScale, -mapToTileScale); // map coordinates to tile indices

            // tile indices of visible rectangle
            Point p1 = transform.Transform(new Point(0d, 0d));
            Point p2 = transform.Transform(new Point(size.Width, 0d));
            Point p3 = transform.Transform(new Point(0d, size.Height));
            Point p4 = transform.Transform(new Point(size.Width, size.Height));

            double left = Math.Min(p1.X, Math.Min(p2.X, Math.Min(p3.X, p4.X)));
            double right = Math.Max(p1.X, Math.Max(p2.X, Math.Max(p3.X, p4.X)));
            double top = Math.Min(p1.Y, Math.Min(p2.Y, Math.Min(p3.Y, p4.Y)));
            double bottom = Math.Max(p1.Y, Math.Max(p2.Y, Math.Max(p3.Y, p4.Y)));

            // index ranges of visible tiles
            int x1 = (int)Math.Floor(left);
            int x2 = (int)Math.Floor(right);
            int y1 = Math.Max((int)Math.Floor(top), 0);
            int y2 = Math.Min((int)Math.Floor(bottom), numTiles - 1);
            Int32Rect grid = new Int32Rect(x1, y1, x2 - x1 + 1, y2 - y1 + 1);

            if (tileZoomLevel != zoom || tileGrid != grid)
            {
                tileZoomLevel = zoom;
                tileGrid = grid;
                transform = GetVisualTransform();

                if (tileLayers != null)
                {
                    foreach (TileLayer tileLayer in tileLayers)
                    {
                        tileLayer.TransformMatrix = transform;
                        tileLayer.UpdateTiles(tileZoomLevel, tileGrid);
                    }
                }
            }
        }

        private void TileLayersChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
        {
            switch (eventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddChildren(eventArgs.NewStartingIndex, eventArgs.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveChildren(eventArgs.OldStartingIndex, eventArgs.OldItems);
                    break;

                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                    RemoveChildren(eventArgs.OldStartingIndex, eventArgs.OldItems);
                    AddChildren(eventArgs.NewStartingIndex, eventArgs.NewItems);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    ClearChildren();
                    if (eventArgs.NewItems != null)
                    {
                        AddChildren(0, eventArgs.NewItems);
                    }
                    break;
            }

            ((Map)VisualParent).OnTileLayersChanged();
        }

        private void AddChildren(int index, IList layers)
        {
            Matrix transform = GetVisualTransform();

            foreach (TileLayer tileLayer in layers)
            {
                Children.Insert(index++, tileLayer);
                tileLayer.TransformMatrix = transform;
                tileLayer.UpdateTiles(tileZoomLevel, tileGrid);
            }
        }

        private void RemoveChildren(int index, IList layers)
        {
            foreach (TileLayer tileLayer in layers)
            {
                tileLayer.ClearTiles();
            }

            Children.RemoveRange(index, layers.Count);
        }

        private void ClearChildren()
        {
            foreach (TileLayer tileLayer in Children)
            {
                tileLayer.ClearTiles();
            }

            Children.Clear();
        }
    }
}
