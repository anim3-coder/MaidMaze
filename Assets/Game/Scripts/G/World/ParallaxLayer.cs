using UnityEngine;
using System.Collections.Generic;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Камера")]
    [Tooltip("Transform камеры. Если не указан, будет использована Main Camera.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Параллакс")]
    [Tooltip("0 = слой стоит на месте в мире, 1 = слой движется вместе с камерой. " +
             "Для дальних слоёв ставьте 0.05–0.3, для ближних 0.5–0.8.")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactor = 0.5f;

    [Tooltip("Включить вертикальный параллакс.")]
    [SerializeField] private bool moveVertical = false;

    [Tooltip("Множитель вертикального параллакса.")]
    [Range(0f, 1f)]
    [SerializeField] private float verticalParallaxFactor = 0.5f;

    [Header("Бесконечный фон по горизонтали")]
    [Tooltip("Включить бесконечную прокрутку. Требует несколько дочерних спрайтов, расположенных в ряд.")]
    [SerializeField] private bool infiniteHorizontal = false;

    [Tooltip("Ширина одного тайла. Если 0, будет взята из SpriteRenderer первого дочернего объекта.")]
    [SerializeField] private float tileWidth = 0f;

    [Header("Pixel Perfect")]
    [Tooltip("Привязывать позицию к пиксельной сетке.")]
    [SerializeField] private bool snapToPixelGrid = true;

    [Tooltip("Pixels Per Unit, как в настройках спрайтов и Pixel Perfect Camera.")]
    [SerializeField] private float pixelsPerUnit = 16f;

    private Vector3 startPosition;
    private Vector3 cameraStartPosition;
    private float totalWidth;
    private List<Transform> tiles = new List<Transform>();

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        startPosition = transform.position;

        // Собираем всех детей как тайлы
        foreach (Transform child in transform)
            tiles.Add(child);

        // Если детей нет, используем сам объект
        if (tiles.Count == 0)
            tiles.Add(transform);

        // Определяем ширину тайла
        if (tileWidth <= 0f)
        {
            SpriteRenderer sr = tiles[0].GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                tileWidth = sr.sprite.bounds.size.x * tiles[0].lossyScale.x;
            else
                tileWidth = 10f; // запасное значение
        }

        totalWidth = tileWidth * tiles.Count;

        // Предупреждение, если включён бесконечный фон, но нет нескольких спрайтов
        if (infiniteHorizontal && tiles.Count == 1)
        {
            Debug.LogWarning("ParallaxLayer: Для бесконечного фона нужно несколько дочерних спрайтов. Infinite Horizontal отключён.", this);
            infiniteHorizontal = false;
        }
    }

    private void Start()
    {
        if (cameraTransform != null)
            cameraStartPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 delta = cameraTransform.position - cameraStartPosition;

        float x = startPosition.x + delta.x * parallaxFactor;
        float y = startPosition.y;

        if (moveVertical)
            y += delta.y * verticalParallaxFactor;

        Vector3 newPos = new Vector3(x, y, startPosition.z);

        if (snapToPixelGrid && pixelsPerUnit > 0f)
        {
            newPos.x = Mathf.Round(newPos.x * pixelsPerUnit) / pixelsPerUnit;
            newPos.y = Mathf.Round(newPos.y * pixelsPerUnit) / pixelsPerUnit;
        }

        transform.position = newPos;

        if (infiniteHorizontal && totalWidth > 0f)
        {
            float halfWidth = totalWidth * 0.5f;

            foreach (Transform tile in tiles)
            {
                float relativeX = tile.position.x - cameraTransform.position.x;

                if (relativeX > halfWidth)
                    tile.position -= new Vector3(totalWidth, 0f, 0f);
                else if (relativeX < -halfWidth)
                    tile.position += new Vector3(totalWidth, 0f, 0f);
            }
        }
    }
}