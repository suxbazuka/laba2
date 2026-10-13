using EnemyEditor;

namespace Clicker
{
    /// <summary>Игровая логика. Окно только вызывает методы и читает состояние.</summary>
    public class CClickerGame
    {
        private readonly EnemySpawner _spawner;

        public CPlayer Player { get; private set; }
        public CEnemy CurrentEnemy { get; private set; }
        public int Stage { get; private set; } // сколько противников побеждено

        public CClickerGame(CEnemyTemplateList templates)
        {
            _spawner = new EnemySpawner(templates);
            Player = new CPlayer();
            Stage = 0;
            SpawnNext();
        }

        public void Click()
        {
            if (CurrentEnemy.TakeDamage(Player.Damage))
            {
                Player.AddGold(CurrentEnemy.Reward);
                Stage++;
                SpawnNext();
            }
        }

        public bool TryUpgradeDamage()
        {
            return Player.TryUpgrade();
        }

        private void SpawnNext()
        {
            CurrentEnemy = new CEnemy(_spawner.NextTemplate(), Stage);
        }
    }
}
