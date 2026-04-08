using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemyController : MonoBehaviour
{

    [SerializeField]
    private List<Enemy> enemyPrefabs;

    private List<Enemy> enemies = new List<Enemy>();
    private float spawnTimer = 0f;
    private const float spawnInterval = 3f;
    private const int maxEnemies = 3;
    private BoxCollider2D spawnCollider;

    private void Awake()
    {
        spawnCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && enemies.Count < maxEnemies)
        {
            Enemy nextEnemy = GetNextEnemy();
            if (nextEnemy != null)
            {
                SpawnEnemy(nextEnemy);
                spawnTimer = 0f;
            }
        }
    }

    private void SpawnEnemy(Enemy prefab)
    {
        Vector2 spawnPosition = GetRandomSpawnPosition();
        Enemy enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
        enemy.OnDeath += () => RemoveEnemy(enemy);
        enemies.Add(enemy);
    }

    private void RemoveEnemy(Enemy enemy)
    {
        enemies.Remove(enemy);
    }

    private Enemy GetNextEnemy()
    {
        if (enemyPrefabs.Count == 0) return null;
        return enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
    }

    private Vector2 GetRandomSpawnPosition()
    {
        Bounds bounds = spawnCollider.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        return new Vector2(x, y);
    }
}
