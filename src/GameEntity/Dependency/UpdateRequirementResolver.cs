using System;

namespace GameEntity
{
    internal enum UpdateRequirementBlockReason
    {
        None = 0,
        NotComponent = 1,
        OwnerMissing = 2,
        ComponentMissing = 3,
        ComponentNotReady = 4,
        ComponentStateError = 5,
    }

    internal readonly struct UpdateRequirementResult
    {
        private UpdateRequirementResult(
            bool hasRequirements,
            UpdateRequirementBlockReason blockReason,
            Type requirementType,
            Exception exception)
        {
            HasRequirements = hasRequirements;
            BlockReason = blockReason;
            RequirementType = requirementType;
            Exception = exception;
        }

        public bool HasRequirements { get; }

        public bool CanUpdate => BlockReason == UpdateRequirementBlockReason.None;

        public UpdateRequirementBlockReason BlockReason { get; }

        public Type RequirementType { get; }

        public Exception Exception { get; }

        public static UpdateRequirementResult Satisfied(bool hasRequirements)
        {
            return new UpdateRequirementResult(hasRequirements, UpdateRequirementBlockReason.None, null, null);
        }

        public static UpdateRequirementResult Blocked(
            UpdateRequirementBlockReason reason,
            Type requirementType = null,
            Exception exception = null)
        {
            return new UpdateRequirementResult(true, reason, requirementType, exception);
        }
    }

    internal static class UpdateRequirementResolver
    {
        public static UpdateRequirementResult Check(Entity entity)
        {
            if (entity == null)
            {
                return UpdateRequirementResult.Satisfied(false);
            }

            Type[] requirementTypes = UpdateRequirementMetadata.GetRequirementTypes(entity.GetType());
            if (requirementTypes.Length == 0)
            {
                return UpdateRequirementResult.Satisfied(false);
            }

            if (!entity.IsComponent)
            {
                return UpdateRequirementResult.Blocked(UpdateRequirementBlockReason.NotComponent);
            }

            Entity owner = entity.Owner;
            if (owner == null || owner.IsDestroyed)
            {
                return UpdateRequirementResult.Blocked(UpdateRequirementBlockReason.OwnerMissing);
            }

            foreach (Type requirementType in requirementTypes)
            {
                Entity requirement = owner.GetComponent(requirementType);
                if (requirement == null || requirement.IsDestroyed)
                {
                    return UpdateRequirementResult.Blocked(
                        UpdateRequirementBlockReason.ComponentMissing,
                        requirementType);
                }

                if (requirement is IEntityReadyState readyState)
                {
                    try
                    {
                        if (!readyState.IsReady)
                        {
                            return UpdateRequirementResult.Blocked(
                                UpdateRequirementBlockReason.ComponentNotReady,
                                requirementType);
                        }
                    }
                    catch (Exception e)
                    {
                        return UpdateRequirementResult.Blocked(
                            UpdateRequirementBlockReason.ComponentStateError,
                            requirementType,
                            e);
                    }
                }
            }

            return UpdateRequirementResult.Satisfied(true);
        }
    }
}
