using AsaSavegameToolkit.Plumbing.Primitives;
using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace AsaSavegameToolkit.Porcelain
{
    public class DroppedItem
    {
        public string ClassName { get; set; } = string.Empty;
        public double? OriginalCreationTime { get; set; }
        public string? DroppedByName { get; set; } 
        public long? DroppedByPlayerId { get; set; }
        public int? TargetingTeam { get; set; } 
        public Item? Item { get; set; }

        public FVector? Location { get; set; }
        public FVector2D? GPSLocation { get; set; }

        public static DroppedItem Create(GameObjectRecord record, ActorTransform? location = null)
        {
            var properties = record.Properties;
            var className = record.GetClassName();
            var originalCreationTime = record.Properties.Get<double>("OriginalCreationTime");
            var droppedByName = record.Properties.Get<string>("DroppedByName");
            var droppedByPlayerId = record.Properties.Get<ulong>("DroppedByPlayerID");
            var targetingTeam = record.Properties.Get<int>("TargetingTeam");

            return new DroppedItem
            {
                ClassName = className,
                OriginalCreationTime = originalCreationTime,
                DroppedByName = droppedByName,
                DroppedByPlayerId = (long)droppedByPlayerId,
                TargetingTeam = targetingTeam,
                Location = location!=null?location.Value.Location:null
            };
        }

        internal void IngestItem(Item item)
        {           
            Item = item;
        }

        internal void UpdateGPSLocation(MapDefinition? map)
        {
            if(map == null) return;
            if (Location == null) return; //no location to calcaulte gps co-ords
            GPSLocation = new FVector2D(map.GetLongitude(Location.Value.X), map.GetLatitude(Location.Value.Y));
        }
    }
}
