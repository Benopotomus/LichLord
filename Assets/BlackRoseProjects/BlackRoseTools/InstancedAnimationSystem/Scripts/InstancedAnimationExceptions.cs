/// <summary>
/// Exceptions that are exclusive for Instanced Animation System
/// </summary>
namespace BlackRoseProjects.InstancedAnimationSystem.Exceptions
{
    /// <summary>
    /// Exception thrown when expected mesh not is set to Read/Write in import settings
    /// </summary>
    public class MeshNotReadableException : System.Exception
    {
        internal MeshNotReadableException(string description) : base(description)
        {
        }
    }

    /// <summary>
    /// Exception thrown while safety check faild inside Baked Animator or Animation managing system
    /// </summary>
    public class SafetyFailException : System.Exception
    {
        internal SafetyFailException(string description) : base(description)
        {
        }
    }

    /// <summary>
    /// Exception thrown while trying to create Instanced Renderer while instancing is disabled
    /// </summary>
    public class InstancingNotEnabledException : System.Exception
    {
        internal InstancingNotEnabledException(string description) : base(description)
        {
        }
    }

    /// <summary>
    /// Exception thrown while trying to create Instanced Renderer over Max Instanced Objects limit. Limit can be set at Project Settings -> Instanced Rendering
    /// </summary>
    public class InstancedRenderersLimitReached : System.Exception
    {
        internal InstancedRenderersLimitReached(string description) : base(description)
        {
        }
    }

    /// <summary>
    /// Exception thrown while trying to start bone synchronization for bone that is already synchronized
    /// </summary>
    public class DuplicateBoneSynchronizationException : System.Exception
    {
        internal DuplicateBoneSynchronizationException(string message) : base(message)
        {
        }
    }
}