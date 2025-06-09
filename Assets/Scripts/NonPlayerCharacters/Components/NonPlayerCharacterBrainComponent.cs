using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterBrainComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;
        public NonPlayerCharacter NPC => _npc;

        public Vector3 _moveTarget;
        public Transform _attackTarget;

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams)
        {
        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            //find closest enemy
            //if (_attackTarget == null)
              //  _attackTarget = findCurrentTarget();
        }
        /*
        public Transform findCurrentTarget()
        {
            //find all potential targets (enemies of this character)
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(attackTag);
            if (enemies.Length == 0)
                return null;

            Transform target = null;

            //if we want this character to communicate with his allies
            if (spread)
            {
                //get all enemies
                List<GameObject> availableEnemies = enemies.ToList();
                int count = 0;

                //make sure it doesn't get stuck in an infinite loop
                while (count < 300)
                {
                    //for all enemies
                    for (int i = 0; i < enemies.Length; i++)
                    {
                        //distance between character and its nearest enemy
                        float closestDistance = Mathf.Infinity;

                        foreach (GameObject potentialTarget in availableEnemies)
                        {
                            //check if there are enemies left to attack and check per enemy if its closest to this character
                            if (Vector3.Distance(transform.position, potentialTarget.transform.position) < closestDistance && potentialTarget != null)
                            {
                                //if this enemy is closest to character, set closest distance to distance between character and enemy
                                closestDistance = Vector3.Distance(transform.position, potentialTarget.transform.position);
                                target = potentialTarget.transform;
                            }
                        }

                        //if it is valid, return this target
                        if (target && canAttack(target))
                        {
                            return target;
                        }
                        else
                        {
                            //if it's not, remove it from the list and try again
                            availableEnemies.Remove(target.gameObject);
                        }
                    }

                    //after checking all enemies, allow one more ally to also attack the same enemy and try again
                    maxAlliesPerEnemy++;
                    availableEnemies.Clear();
                    availableEnemies = enemies.ToList();

                    count++;
                }

                //show a loop error
                Debug.LogError("Infinite loop");
            }
            else
            {
                //if we're using the simple method:
                float closestDistance = Mathf.Infinity;

                foreach (GameObject potentialTarget in enemies)
                {
                    //check if there are enemies left to attack and check per enemy if its closest to this character
                    if (Vector3.Distance(transform.position, potentialTarget.transform.position) < closestDistance && potentialTarget != null)
                    {
                        //if this enemy is closest to character, set closest distance to distance between character and enemy
                        closestDistance = Vector3.Distance(transform.position, potentialTarget.transform.position);
                        target = potentialTarget.transform;
                    }
                }

                //check if there's a target and return it
                if (target)
                    return target;
            }

            //otherwise return null
            return null;
        }
        */
    }
}
