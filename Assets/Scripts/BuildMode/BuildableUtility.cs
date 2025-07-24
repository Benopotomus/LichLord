using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord.Buildables
{
    public static class BuildableUtility
    {
        public static void GetWallWorldTransform(
             BuildableZone zone,
             int x,
             int y,
             int z,
             EWallOrientation orientation,
             out Vector3 worldPosition,
             out Quaternion rotation)
        {
            if (zone == null || zone.Grid == null)
            {
                worldPosition = Vector3.zero;
                rotation = Quaternion.identity;
                Debug.LogWarning("BuildableZone or its Grid is null.");
                return;
            }

            float tileSizeXZ = zone.Grid.TileSizeXZ;
            float tileSizeY = zone.Grid.TileSizeY;

            float halfSizeXZ = tileSizeXZ * 0.5f;
            float wallBaseY = y * tileSizeY;

            Vector3 wallCenterLocal = Vector3.zero;

            switch (orientation)
            {
                case EWallOrientation.North:
                    wallCenterLocal = new Vector3(
                        x * tileSizeXZ + halfSizeXZ,
                        wallBaseY,
                        (z + 1) * tileSizeXZ
                    );
                    break;

                case EWallOrientation.South:
                    wallCenterLocal = new Vector3(
                        x * tileSizeXZ + halfSizeXZ,
                        wallBaseY,
                        z * tileSizeXZ
                    );
                    break;

                case EWallOrientation.East:
                    wallCenterLocal = new Vector3(
                        (x + 1) * tileSizeXZ,
                        wallBaseY,
                        z * tileSizeXZ + halfSizeXZ
                    );
                    break;

                case EWallOrientation.West:
                    wallCenterLocal = new Vector3(
                        x * tileSizeXZ,
                        wallBaseY,
                        z * tileSizeXZ + halfSizeXZ
                    );
                    break;

                default:
                    Debug.LogWarning($"Unknown wall orientation {orientation}");
                    break;
            }

            worldPosition = zone.transform.TransformPoint(wallCenterLocal);
            rotation = zone.transform.rotation * GetRotationForWall(orientation);
        }

        private static Quaternion GetRotationForWall(EWallOrientation orientation)
        {
            return orientation switch
            {
                EWallOrientation.North => Quaternion.identity,
                EWallOrientation.East => Quaternion.Euler(0, 90, 0),
                EWallOrientation.South => Quaternion.Euler(0, 180, 0),
                EWallOrientation.West => Quaternion.Euler(0, 270, 0),
                _ => Quaternion.identity
            };
        }

        public static void GetFloorWorldTransform(
            BuildableZone zone,
            int x,
            int y,
            int z,
            out Vector3 worldPosition,
            out Quaternion rotation)
        {
            if (zone == null || zone.Grid == null)
            {
                worldPosition = Vector3.zero;
                rotation = Quaternion.identity;
                Debug.LogWarning("BuildableZone or its Grid is null.");
                return;
            }

            float tileSizeXZ = zone.Grid.TileSizeXZ;
            float tileSizeY = zone.Grid.TileSizeY;

            // center of the floor tile
            float halfSizeXZ = tileSizeXZ * 0.5f;

            Vector3 floorCenterLocal = new Vector3(
                x * tileSizeXZ + halfSizeXZ,
                y * tileSizeY,
                z * tileSizeXZ + halfSizeXZ
            );

            worldPosition = zone.transform.TransformPoint(floorCenterLocal);
            rotation = zone.transform.rotation; // floors are flat on the grid
        }

        public static void GetFeatureWorldTransform(
                BuildableZone zone,
                int subTileX,
                int y,
                int subTileZ,
                out Vector3 worldPosition,
                out Quaternion rotation)
        {
            if (zone == null || zone.Grid == null)
            {
                worldPosition = Vector3.zero;
                rotation = Quaternion.identity;
                Debug.LogWarning("BuildableZone or its Grid is null.");
                return;
            }

            float tileSizeXZ = zone.Grid.TileSizeXZ;
            float tileSizeY = zone.Grid.TileSizeY;

            // center of the floor tile
            float halfSizeXZ = tileSizeXZ * 0.5f;

            Vector3 floorCenterLocal = new Vector3(
                subTileX * tileSizeXZ + halfSizeXZ,
                y * tileSizeY,
                subTileZ * tileSizeXZ + halfSizeXZ
            );

            worldPosition = zone.transform.TransformPoint(floorCenterLocal);
            rotation = zone.transform.rotation; // floors are flat on the grid
        }
    }
}
