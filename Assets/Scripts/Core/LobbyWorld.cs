// LobbyWorld.cs — 3D-мир зала ожидания (гейт аэропорта) для сцены Lobby.

using UnityEngine;

/// <summary>Зал ожидания аэропорта, где экипаж собирается перед рейсом.</summary>
public class LobbyWorld : MonoBehaviour
{
    private void Awake()
    {
        // Свет и небо.
        var sunGo = new GameObject("LobbySun");
        sunGo.transform.rotation = Quaternion.Euler(55f, -40f, 0f);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.97f, 0.9f);
        sun.intensity = 1.05f;
        sun.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.6f, 0.65f, 0.72f);
        RenderSettings.ambientEquatorColor = new Color(0.5f, 0.5f, 0.48f);
        RenderSettings.ambientGroundColor = new Color(0.3f, 0.28f, 0.25f);
        var skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            var sky = new Material(skyShader);
            sky.SetColor("_SkyTint", new Color(0.5f, 0.6f, 0.8f));
            RenderSettings.skybox = sky;
        }

        var floorMat = ProceduralAssets.Mat("LobbyFloor", new Color(0.42f, 0.4f, 0.38f));
        var wallMat = ProceduralAssets.Mat("LobbyWall", new Color(0.72f, 0.7f, 0.66f));
        var seatMat = ProceduralAssets.Mat("LobbySeat", new Color(0.2f, 0.35f, 0.5f));
        var metalMat = ProceduralAssets.Mat("LobbyMetal", ProceduralAssets.Palette.Metal);
        var signMat = ProceduralAssets.Mat("LobbySign", new Color(0.1f, 0.12f, 0.14f));

        // Пол 30×30.
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(transform, false);
        floor.transform.localScale = new Vector3(6f, 1f, 6f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

        // Стены.
        ProceduralAssets.Box(transform, "WallN", new Vector3(0f, 2.5f, 15f), Vector3.zero, new Vector3(30f, 5f, 0.4f), wallMat, collider: true);
        ProceduralAssets.Box(transform, "WallS", new Vector3(0f, 2.5f, -15f), Vector3.zero, new Vector3(30f, 5f, 0.4f), wallMat, collider: true);
        ProceduralAssets.Box(transform, "WallW", new Vector3(-15f, 2.5f, 0f), Vector3.zero, new Vector3(0.4f, 5f, 30f), wallMat, collider: true);
        ProceduralAssets.Box(transform, "WallE", new Vector3(15f, 2.5f, 0f), Vector3.zero, new Vector3(0.4f, 5f, 30f), wallMat, collider: true);

        // Ряды кресел.
        for (int row = 0; row < 3; row++)
        {
            for (int i = 0; i < 6; i++)
            {
                var seat = new GameObject($"Chair{row}_{i}");
                seat.transform.SetParent(transform, false);
                seat.transform.localPosition = new Vector3(-7.5f + i * 3f, 0f, -8f + row * 2.5f);
                seat.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
                ProceduralAssets.Box(seat.transform, "Base", new Vector3(0f, 0.45f, 0f), Vector3.zero,
                    new Vector3(1.2f, 0.2f, 1.1f), seatMat);
                ProceduralAssets.Box(seat.transform, "Back", new Vector3(0f, 0.85f, 0.5f), Vector3.zero,
                    new Vector3(1.2f, 0.9f, 0.18f), seatMat);
            }
        }

        // Табло «Вылет» (текст обновит LobbyManager).
        var board = new GameObject("DeparturesBoard");
        board.transform.SetParent(transform, false);
        board.transform.localPosition = new Vector3(0f, 3.4f, -14.5f);
        ProceduralAssets.Box(board.transform, "Frame", Vector3.zero, Vector3.zero, new Vector3(10f, 2.4f, 0.3f), signMat);
        var textGo = new GameObject("BoardText");
        textGo.transform.SetParent(board.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 0f, -0.2f);
        var tm = textGo.AddComponent<TextMesh>();
        tm.fontSize = 34;
        tm.characterSize = 0.14f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.85f, 0.3f);
        textGo.AddComponent<MeshRenderer>().sharedMaterial = ProceduralAssets.BuiltinFont.material;
        var holder = board.AddComponent<BoardTextHolder>();
        holder.textMesh = tm;

        // Стойка регистрации и фикус для уюта.
        ProceduralAssets.Box(transform, "Desk", new Vector3(-10f, 0.55f, 8f), Vector3.zero,
            new Vector3(4f, 1.1f, 1.2f), metalMat);
        var ficus = ProceduralAssets.Cylinder(transform, "FicusPot", new Vector3(10f, 0.3f, 8f), Vector3.zero,
            0.4f, 0.6f, ProceduralAssets.Mat("Pot", new Color(0.5f, 0.3f, 0.2f)));
        ProceduralAssets.Sphere(ficus.transform, "Leaves", new Vector3(0f, 0.8f, 0f), 0.7f,
            ProceduralAssets.Mat("Leaves", new Color(0.2f, 0.45f, 0.2f)));

        // Точка спавна игроков.
        NetworkLauncher.SpawnPoint = new Vector3(0f, 0.1f, 4f);
        NetworkLauncher.SpawnYaw = 0f;
    }
}

/// <summary>Держатель ссылки на TextMesh табло (для LobbyManager).</summary>
public class BoardTextHolder : MonoBehaviour
{
    public TextMesh textMesh;
}
