using Luxodd.Game.Scripts.HelpersAndUtils;

namespace Game.Enemy
{
    public class EnemyDroneXBehaviour : EnemyBaseBehaviour
    {
        protected override void OnDestroyStateHandler()
        {
            CoroutineManager.NextFrameAction(1, () => { Destroy(gameObject); });
        }
    }
}