using System.Collections.Generic;
using UnityEngine;

namespace Digit7s.Enemies
{
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Digit7s/Enemies/Enemy Database")]
    public class EnemyDatabase : ScriptableObject
    {
        [Header("Collections")]
        [Tooltip("Collection containing all Regular (ရိုးရိုး) enemies")]
        public EnemyCollection regularEnemiesCollection;

        [Tooltip("Collection containing all Boss enemies")]
        public EnemyCollection bossEnemiesCollection;

        public int RegularEnemyCount => regularEnemiesCollection != null ? regularEnemiesCollection.Count : 0;
        public int BossEnemyCount => bossEnemiesCollection != null ? bossEnemiesCollection.Count : 0;
        public int TotalEnemyCount => RegularEnemyCount + BossEnemyCount;

        public EnemyData GetRegularEnemy(int index) => regularEnemiesCollection != null ? regularEnemiesCollection.GetEnemy(index) : null;
        public EnemyData GetBossEnemy(int index) => bossEnemiesCollection != null ? bossEnemiesCollection.GetEnemy(index) : null;

        public EnemyData GetEnemyById(string id)
        {
            if (regularEnemiesCollection != null)
            {
                var reg = regularEnemiesCollection.GetEnemyById(id);
                if (reg != null) return reg;
            }

            if (bossEnemiesCollection != null)
            {
                var boss = bossEnemiesCollection.GetEnemyById(id);
                if (boss != null) return boss;
            }

            return null;
        }

        public List<EnemyData> GetAllEnemies()
        {
            var list = new List<EnemyData>();
            if (regularEnemiesCollection != null && regularEnemiesCollection.enemies != null)
            {
                list.AddRange(regularEnemiesCollection.enemies);
            }
            if (bossEnemiesCollection != null && bossEnemiesCollection.enemies != null)
            {
                list.AddRange(bossEnemiesCollection.enemies);
            }
            return list;
        }
    }
}
