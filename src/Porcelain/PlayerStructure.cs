using AsaSavegameToolkit.Plumbing.Records;
using AsaSavegameToolkit.Plumbing.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsaSavegameToolkit.Porcelain
{
    public class PlayerStructure: Structure
    {
        public long TribeId { get; set; }
        public string TribeName { get; set; } = string.Empty;
        public long OwningPlayerId { get; set; } = 0;
        public long PlacerId { get; set; } = 0;
        public string PlacedTime { get; set; } = string.Empty;
        
        internal double CreationGameTime { get; set; } = 0;
        internal double LastAllyInRangeGameTime { get; set; } = 0;
        
        public DateTime? CreationTimestamp { get; set; }
        public DateTime? LastAllyInRangeTimestamp { get; set; }

        public bool HasFuel { get; set; } = false;
        public bool LastToggleActivated { get; set; } =false;
        public bool IsPinLocked { get; set; } = false;
        public bool IsLocked { get; set; } = false;
        public bool IsWatered { get; set; } = false;
        public bool IsPowered { get; set; } = false;
        public bool IsSeeded { get; set; } = false;
        public bool IsFertilized { get; set; } = false;

        public static new PlayerStructure Create(GameObjectRecord record, ActorTransform? transform)
        {
            var className = record.GetClassName();
            var properties = record.Properties;

            var targetingTeam = properties.Get<int>("TargetingTeam");            
            var structureId = properties.Get<uint>("StructureID");
            var containerActivated = properties.Get<bool>("bContainerActivated");
            var lastActivatedTime = properties.Get<double>("LastActivatedTime");
            var lastDeactivatedTime = properties.Get<double>("LastDeactivatedTime");
            var displayName = properties.Get<string>("BoxName") ?? "";


            var ownerName = properties.Get<string>("OwnerName") ?? "";
            var owningPlayerId = properties.Get<int>("OwningPlayerID");
            var originalPlacerId = properties.Get<int>("OriginalPlacerID");
            var originalPlacedTimeStamp = properties.Get<string>("OriginalPlacedTimeStamp")??"";
            var originalCreationTime = properties.Get<double>("OriginalCreationTime");
            var lastInAllyRangeTimeSerialized = properties.Get<double>("LastAllyInRangeTimeSerialized");

            var hasFuel = properties.Get<bool>("bHasFuel");
            var lastToggleActivated = properties.Get<bool>("bLastToggleActivated");
            var isPinLocked = properties.Get<bool>("bIsPinLocked");
            var isLocked = properties.Get<bool>("bIsLocked");
            var isWatered = properties.Get<bool>("bIsWatered");
            var isPowered = properties.Get<bool>("bIsPinPowered");
            var isSeeded = properties.Get<bool>("bIsSeeded");
            var isFertilized = properties.Get<bool>("bIsFertilized");
            var hasFruitItems = properties.Get<bool>("bHasFruitItems");


            return new PlayerStructure()
            {
                Id = record.Uuid,
                ClassName = className,
                StructureName = displayName,
                IsActivated = containerActivated,
                LastActivatedGameTime = lastActivatedTime,
                LastDeactivatedGameTime = lastDeactivatedTime,
                TribeId = targetingTeam,
                TribeName = ownerName,
                CreationGameTime = originalCreationTime,
                HasFuel = hasFuel,
                IsFertilized = isFertilized,
                IsPinLocked = isPinLocked,
                IsLocked = isLocked,
                IsSeeded = isSeeded,
                IsPowered = isPowered,
                IsWatered = isWatered,
                LastAllyInRangeGameTime = lastInAllyRangeTimeSerialized,
                OwningPlayerId = owningPlayerId,
                LastToggleActivated = lastToggleActivated,
                PlacedTime = originalPlacedTimeStamp,
                PlacerId = originalPlacerId,
                Location = transform?.Location,
                Rotation = transform?.Rotation
            };
        }

        public override void IngestInventory(Inventory inventory)
        {
            Inventory = inventory;
        }

        public override string ToString()
        {
            var name = StructureName ?? ClassName;
            var tribe = TribeName != null ? $" [{TribeName}]" : "";
            return $"{name}{tribe}";
        }


        internal override void RefreshTimestamps(DateTime saveTimestamp, double gameTime)
        {
            if (LastActivatedGameTime > 0)
            {
                LastActivatedTimestamp = saveTimestamp.AddSeconds(LastActivatedGameTime - gameTime);
            }

            if (LastDeactivatedGameTime > 0)
            {
                LastDeactivatedTimestamp = saveTimestamp.AddSeconds(LastDeactivatedGameTime - gameTime);
            }

            if (CreationGameTime > 0)
            {
                CreationTimestamp = saveTimestamp.AddSeconds(CreationGameTime - gameTime);
            }

            if (LastAllyInRangeGameTime > 0)
            {
                LastAllyInRangeTimestamp = saveTimestamp.AddSeconds(LastAllyInRangeGameTime - gameTime);
            }

        }

    }
}
