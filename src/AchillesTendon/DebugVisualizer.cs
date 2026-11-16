using System.Collections.Generic;
using AchillesTendon.Logic;
using ThunderRoad;
using UnityEngine;

namespace AchillesTendon
{
    /// <summary>
    /// Runtime debug drawing. Unity gizmos do not render in a built game, so the
    /// heel zones and hit markers are plain sphere meshes without colliders.
    /// </summary>
    internal sealed class DebugVisualizer
    {
        private const float HitMarkerDiameter = 0.03f;

        // Shader candidates, first found wins. Which ones exist in the game build
        // could not be verified offline; the chosen one is logged once.
        private static readonly string[] ShaderCandidates =
        {
            "Sprites/Default",
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
        };

        private static readonly Color ZoneColor = new Color(1f, 0.85f, 0f, 0.35f);
        private static readonly Color HeelHitColor = new Color(0f, 1f, 0f, 0.9f);
        private static readonly Color MissColor = new Color(1f, 0f, 0f, 0.9f);

        private readonly Dictionary<Creature, GameObject[]> zoneMarkers = new Dictionary<Creature, GameObject[]>();
        private readonly List<KeyValuePair<GameObject, float>> hitMarkers = new List<KeyValuePair<GameObject, float>>();
        private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        private readonly List<Creature> staleCreatures = new List<Creature>();

        private Shader shader;
        private bool shaderResolved;

        public void Update()
        {
            UpdateZones();
            ExpireHitMarkers();
        }

        public void AddHitMarker(Vector3 position, bool isHeelHit)
        {
            if (!ModOptions.ShowHitMarkers)
            {
                return;
            }
            var marker = CreateSphere("AchillesTendon.HitMarker", isHeelHit ? HeelHitColor : MissColor);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * HitMarkerDiameter;
            hitMarkers.Add(new KeyValuePair<GameObject, float>(marker, Time.time + ModOptions.HitMarkerDuration));
        }

        public void Clear()
        {
            foreach (var markers in zoneMarkers.Values)
            {
                DestroyAll(markers);
            }
            zoneMarkers.Clear();
            foreach (var entry in hitMarkers)
            {
                Destroy(entry.Key);
            }
            hitMarkers.Clear();
        }

        private void UpdateZones()
        {
            if (!ModOptions.ShowHeelZones || !ModOptions.TendonEnabled)
            {
                if (zoneMarkers.Count > 0)
                {
                    foreach (var markers in zoneMarkers.Values)
                    {
                        DestroyAll(markers);
                    }
                    zoneMarkers.Clear();
                }
                return;
            }

            var settings = ModOptions.CurrentHeelHitSettings();
            foreach (var creature in Creature.allActive)
            {
                if (creature == null || creature.isKilled || (creature.isPlayer && !settings.ApplyToPlayer))
                {
                    continue;
                }
                if (!zoneMarkers.TryGetValue(creature, out var markers))
                {
                    markers = new[]
                    {
                        CreateSphere("AchillesTendon.HeelZone.Left", ZoneColor),
                        CreateSphere("AchillesTendon.HeelZone.Right", ZoneColor),
                    };
                    zoneMarkers[creature] = markers;
                }
                PlaceZone(markers[0], creature, LegSide.Left, settings);
                PlaceZone(markers[1], creature, LegSide.Right, settings);
            }

            staleCreatures.Clear();
            foreach (var pair in zoneMarkers)
            {
                var creature = pair.Key;
                if (creature == null
                    || creature.isKilled
                    || (creature.isPlayer && !ModOptions.ApplyToPlayer)
                    || !Creature.allActive.Contains(creature))
                {
                    staleCreatures.Add(creature);
                }
            }
            foreach (var creature in staleCreatures)
            {
                DestroyAll(zoneMarkers[creature]);
                zoneMarkers.Remove(creature);
            }
        }

        private static void PlaceZone(GameObject marker, Creature creature, LegSide side, HeelHitSettings settings)
        {
            var foot = GameAdapters.FootOf(creature, side);
            if (foot == null)
            {
                marker.SetActive(false);
                return;
            }
            var center = HeelZone.Center(
                GameAdapters.ToVec3(foot.transform.position),
                GameAdapters.ToVec3(creature.transform.forward),
                settings.ZoneBackOffset,
                settings.ZoneHeightOffset);
            marker.SetActive(true);
            marker.transform.position = GameAdapters.ToVector3(center);
            marker.transform.localScale = Vector3.one * (settings.ZoneRadius * 2f);
        }

        private void ExpireHitMarkers()
        {
            var now = Time.time;
            for (var i = hitMarkers.Count - 1; i >= 0; i--)
            {
                if (hitMarkers[i].Value <= now)
                {
                    Destroy(hitMarkers[i].Key);
                    hitMarkers.RemoveAt(i);
                }
            }
        }

        private GameObject CreateSphere(string name, Color color)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            // Immediately, so the marker never takes part in a physics step.
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            var material = MaterialFor(color);
            if (material != null)
            {
                sphere.GetComponent<Renderer>().sharedMaterial = material;
            }
            return sphere;
        }

        private Material MaterialFor(Color color)
        {
            if (materials.TryGetValue(color, out var material))
            {
                return material;
            }
            var found = ResolveShader();
            if (found == null)
            {
                return null;
            }
            material = new Material(found);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            materials[color] = material;
            return material;
        }

        private Shader ResolveShader()
        {
            if (shaderResolved)
            {
                return shader;
            }
            shaderResolved = true;
            foreach (var candidate in ShaderCandidates)
            {
                shader = Shader.Find(candidate);
                if (shader != null)
                {
                    ModLog.Info("Debug markers use shader: " + candidate);
                    return shader;
                }
            }
            ModLog.Warn("No debug shader found (" + string.Join(", ", ShaderCandidates) + "); markers keep Unity's default material.");
            return null;
        }

        private static void DestroyAll(GameObject[] objects)
        {
            foreach (var obj in objects)
            {
                Destroy(obj);
            }
        }

        private static void Destroy(GameObject obj)
        {
            if (obj != null)
            {
                Object.Destroy(obj);
            }
        }
    }
}
