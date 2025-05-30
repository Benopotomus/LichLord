
/// <summary>
/// Holds scene specific references and common runtime data.
/// </summary>
/// 
namespace LichLord
{
    using Fusion;
    using LichLord.Projectiles;
    using LichLord.UI;

    [System.Serializable]
    public class SceneContext
    {
        public bool IsVisible;
        public bool HasInput;
        public string PeerUserID;

        //public SceneUI UI;
        public NetworkRunner Runner;
        public NetworkGame NetworkGame;
        public SpawnManager SpawnManager;
        public ProjectileManager ProjectileManager;
        public SceneCamera Camera;

        // General
        /*
        public ObjectCache ObjectCache;
        //public GeneralInput GeneralInput;

        public Matchmaking Matchmaking;

        public SceneInput Input;

        public ActorEventManager ActorEventManager;

        public ImpactManager ImpactManager;
        public GameplayEffectManager GameplayEffectManager;
        public LevelManager LevelManager;
        public PropManager PropManager;
        public CreatureManager CreatureManager;
        public AstarPath Pathfinding;
        public HitManager HitManager;


        // Gameplay

        [HideInInspector]

        [HideInInspector]
        public PlayerRef LocalPlayerRef;
        [HideInInspector]
        public PlayerRef ObservedPlayerRef;
        [HideInInspector]
        public HeroEntity ObservedHeroEntity;
        [HideInInspector]
        public GlobalSettings Settings;
        [HideInInspector]
        public RuntimeSettings RuntimeSettings;
        [HideInInspector]
        public GameplayMode GameplayMode;
        [HideInInspector]
        public NetworkLobby Lobby;
                */



        public bool IsGameplayActive()
        {
            return true;
            /*
            if (GameplayMode == null)
                return false;

            if (GameplayMode.GamePhaseStateMachine.PhaseName != eGameplayModePhase.Active)
                return false;

            return true;
            */
        }    
    }
}
