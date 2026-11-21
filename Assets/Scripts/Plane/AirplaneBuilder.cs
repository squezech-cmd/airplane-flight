// AirplaneBuilder.cs — процедурная постройка самолёта: фюзеляж, крылья, двигатели с винтами,
// хвост, кабина с штурвалом, салон с рядами кресел, буфет, дверь, крыло для выхода.
// Самолёт неподвижен в начале координат — мир движется мимо него (см. GameWorld).

using UnityEngine;

/// <summary>Ссылки на построенный самолёт (используют контроллеры и спавнеры).</summary>
public class AirplaneRefs
{
    public Transform Root;
    public Transform Exterior;        // внешние части (для вибраций)
    public Transform PilotSeat;       // кресло пилота (якорь камеры)
    public Transform CopilotSeat;
    public SteeringWheel Yoke;        // штурвал
    public DoorInteraction MainDoor;  // левая передняя дверь (посадка/выброс)
    public WingHatch WingHatch;       // правый люк выхода на крыло
    public Interactable Galley;       // буфет с водой
    public Transform[] Seats = new Transform[24]; // якоря кресел пассажиров
    public Transform EngineL, EngineR;   // гондолы двигателей
    public Transform PropL, PropR;       // винты
    public Transform SmokeL, SmokeR;     // точки дыма при пожаре
    public Transform CargoArea;          // грузовой отсек (визуал)
    public Transform HoseAnchor;         // где лежит заправочный шланг (на крыле)
}

/// <summary>Построитель самолёта.</summary>
public static class AirplaneBuilder
{
    /// <summary>Высота пола салона (мировая Y внутри самолёта).</summary>
    public const float FloorY = -0.9f;

    /// <summary>Границы зоны хождения по крылу (правое крыло).</summary>
    public static readonly Rect WingArea = new Rect(1.0f, -0.6f, 6.6f, 2.6f);

    /// <summary>Построить самолёт в указанный корень и заполнить ссылки.</summary>
    public static AirplaneRefs Build(Transform root, bool boardingScene)
    {
        var refs = new AirplaneRefs { Root = root };

        var fusMat = ProceduralAssets.Mat("Fuselage", ProceduralAssets.Palette.Fuselage);
        var accentMat = ProceduralAssets.Mat("AccentStripe", ProceduralAssets.Palette.Accent);
        var metalMat = ProceduralAssets.Mat("Metal", ProceduralAssets.Palette.Metal);
        var darkMat = ProceduralAssets.Mat("DarkMetal", ProceduralAssets.Palette.DarkMetal);
        var interiorMat = ProceduralAssets.Mat("Interior", ProceduralAssets.Palette.Interior);
        var seatMat = ProceduralAssets.Mat("Seat", ProceduralAssets.Palette.SeatBlue);
        var floorMat = ProceduralAssets.Mat("Floor", ProceduralAssets.Palette.FloorDark);
        var glassMat = ProceduralAssets.Mat("Glass", ProceduralAssets.Palette.Glass, lit: false);

        // ============ ВНЕШНИЙ КОНТУР ============
        var ext = new GameObject("Exterior").transform;
        ext.SetParent(root, false);
        refs.Exterior = ext;

        // Фюзеляж — цилиндр вдоль Z (радиус 1.35, длина 15).
        ProceduralAssets.Cylinder(ext, "Fuselage", new Vector3(0f, 0f, 0f), new Vector3(90f, 0f, 0f),
            1.35f, 15f, fusMat);
        // Оранжая полоса вдоль борта.
        ProceduralAssets.Box(ext, "StripeL", new Vector3(-1.33f, 0.28f, 0f), Vector3.zero,
            new Vector3(0.06f, 0.28f, 14.4f), accentMat);
        ProceduralAssets.Box(ext, "StripeR", new Vector3(1.33f, 0.28f, 0f), Vector3.zero,
            new Vector3(0.06f, 0.28f, 14.4f), accentMat);
        // Носовой обтекатель.
        ProceduralAssets.Sphere(ext, "Nose", new Vector3(0f, 0f, 7.4f), 1.36f, fusMat);
        // Хвостовая часть.
        ProceduralAssets.Sphere(ext, "TailCap", new Vector3(0f, 0.35f, -7.4f), 1.2f, fusMat);
        // Киль.
        ProceduralAssets.Box(ext, "Fin", new Vector3(0f, 2.2f, -6.7f), new Vector3(-12f, 0f, 0f),
            new Vector3(0.16f, 2.4f, 1.9f), accentMat);
        // Стабилизаторы.
        ProceduralAssets.Box(ext, "StabL", new Vector3(-2.6f, 0.6f, -6.6f), Vector3.zero,
            new Vector3(4.6f, 0.14f, 1.5f), fusMat);
        ProceduralAssets.Box(ext, "StabR", new Vector3(2.6f, 0.6f, -6.6f), Vector3.zero,
            new Vector3(4.6f, 0.14f, 1.5f), fusMat);

        // Крылья (с лёгким V).
        var wingL = ProceduralAssets.Box(ext, "WingL", new Vector3(-6.3f, 0.28f, 0.7f), new Vector3(0f, 0f, 4f),
            new Vector3(10.2f, 0.22f, 2.3f), fusMat);
        var wingR = ProceduralAssets.Box(ext, "WingR", new Vector3(6.3f, 0.28f, 0.7f), new Vector3(0f, 0f, -4f),
            new Vector3(10.2f, 0.22f, 2.3f), fusMat);
        // Законцовки.
        ProceduralAssets.Box(wingL.transform, "TipL", new Vector3(-5.0f, 0.22f, 0f), Vector3.zero,
            new Vector3(0.3f, 0.5f, 2.1f), accentMat);
        ProceduralAssets.Box(wingR.transform, "TipR", new Vector3(5.0f, 0.22f, 0f), Vector3.zero,
            new Vector3(0.3f, 0.5f, 2.1f), accentMat);

        // Двигатели (гондолы + винты).
        refs.EngineL = BuildEngine(ext, new Vector3(-3.4f, -0.1f, 1.35f), metalMat, darkMat, out var propL);
        refs.EngineR = BuildEngine(ext, new Vector3(3.4f, -0.1f, 1.35f), metalMat, darkMat, out var propR);
        refs.PropL = propL;
        refs.PropR = propR;

        // Точки дыма.
        var smokeL = new GameObject("SmokeL").transform;
        smokeL.SetParent(refs.EngineL, false);
        smokeL.localPosition = new Vector3(0f, 0.2f, 1.2f);
        refs.SmokeL = smokeL;
        var smokeR = new GameObject("SmokeR").transform;
        smokeR.SetParent(refs.EngineR, false);
        smokeR.localPosition = new Vector3(0f, 0.2f, 1.2f);
        refs.SmokeR = smokeR;

        // Шасси (видно на стоянке).
        var gear = new GameObject("Gear").transform;
        gear.SetParent(ext, false);
        ProceduralAssets.Cylinder(gear, "GearNose", new Vector3(0f, -1.75f, 5.6f), Vector3.zero, 0.09f, 1.6f, metalMat);
        ProceduralAssets.Cylinder(gear, "GearL", new Vector3(-1.4f, -1.75f, 0.4f), Vector3.zero, 0.1f, 1.7f, metalMat);
        ProceduralAssets.Cylinder(gear, "GearR", new Vector3(1.4f, -1.75f, 0.4f), Vector3.zero, 0.1f, 1.7f, metalMat);
        ProceduralAssets.Cylinder(gear, "WheelN", new Vector3(0f, -2.45f, 5.6f), new Vector3(0f, 0f, 90f), 0.28f, 0.18f, darkMat);
        ProceduralAssets.Cylinder(gear, "WheelL", new Vector3(-1.4f, -2.55f, 0.4f), new Vector3(0f, 0f, 90f), 0.34f, 0.22f, darkMat);
        ProceduralAssets.Cylinder(gear, "WheelR", new Vector3(1.4f, -2.55f, 0.4f), new Vector3(0f, 0f, 90f), 0.34f, 0.22f, darkMat);
        gear.gameObject.SetActive(boardingScene);

        // Иллюминаторы — светящиеся точки по бортам.
        var windowMat = ProceduralAssets.Mat("PaxWindow", new Color(0.75f, 0.85f, 0.95f), lit: false);
        for (int i = 0; i < 9; i++)
        {
            float z = -4.6f + i * 1.15f;
            ProceduralAssets.Box(ext, $"WinL{i}", new Vector3(-1.36f, 0.35f, z), Vector3.zero,
                new Vector3(0.04f, 0.34f, 0.5f), windowMat);
            ProceduralAssets.Box(ext, $"WinR{i}", new Vector3(1.36f, 0.35f, z), Vector3.zero,
                new Vector3(0.04f, 0.34f, 0.5f), windowMat);
        }

        // ============ ИНТЕРЬЕР ============
        var interior = new GameObject("Interior").transform;
        interior.SetParent(root, false);

        // Пол (коллайдер — по нему ходим).
        var floor = ProceduralAssets.Box(interior, "Floor", new Vector3(0f, FloorY - 0.06f, -0.5f), Vector3.zero,
            new Vector3(2.6f, 0.12f, 13.2f), floorMat, collider: true);
        // Кабина (продолжение пола вперёд).
        ProceduralAssets.Box(interior, "CockpitFloor", new Vector3(0f, FloorY - 0.06f, 5.6f), Vector3.zero,
            new Vector3(2.0f, 0.12f, 3.2f), floorMat, collider: true);

        // Потолок.
        ProceduralAssets.Box(interior, "Ceiling", new Vector3(0f, 1.32f, -0.5f), Vector3.zero,
            new Vector3(2.6f, 0.1f, 13.0f), interiorMat);
        // Боковые стены (с проходом к дверям на участках).
        ProceduralAssets.Box(interior, "WallL", new Vector3(-1.34f, 0.15f, -0.5f), Vector3.zero,
            new Vector3(0.1f, 2.9f, 13.0f), interiorMat, collider: true);
        ProceduralAssets.Box(interior, "WallR", new Vector3(1.34f, 0.15f, -0.5f), Vector3.zero,
            new Vector3(0.1f, 2.9f, 13.0f), interiorMat, collider: true);
        // Задняя перегородка.
        ProceduralAssets.Box(interior, "BackWall", new Vector3(0f, 0.15f, -6.1f), Vector3.zero,
            new Vector3(2.6f, 2.9f, 0.1f), interiorMat, collider: true);
        // Перегородка кабины (с проходом по центру).
        ProceduralAssets.Box(interior, "BulkL", new Vector3(-0.85f, 0.15f, 4.15f), Vector3.zero,
            new Vector3(0.9f, 2.9f, 0.1f), interiorMat, collider: true);
        ProceduralAssets.Box(interior, "BulkR", new Vector3(0.85f, 0.15f, 4.15f), Vector3.zero,
            new Vector3(0.9f, 2.9f, 0.1f), interiorMat, collider: true);
        ProceduralAssets.Box(interior, "BulkTop", new Vector3(0f, 0.85f, 4.15f), Vector3.zero,
            new Vector3(0.85f, 1.2f, 0.1f), interiorMat, collider: true);

        // Багажные полки.
        ProceduralAssets.Box(interior, "RackL", new Vector3(-1.0f, 1.0f, -0.5f), Vector3.zero,
            new Vector3(0.62f, 0.34f, 12.4f), interiorMat);
        ProceduralAssets.Box(interior, "RackR", new Vector3(1.0f, 1.0f, -0.5f), Vector3.zero,
            new Vector3(0.62f, 0.34f, 12.4f), interiorMat);

        // Ряды кресел: 6 рядов × 4 места.
        int seatIndex = 0;
        for (int row = 0; row < 6; row++)
        {
            float z = 3.3f - row * 1.5f;
            foreach (float x in new[] { -1.02f, -0.5f, 0.5f, 1.02f })
            {
                var seat = BuildSeat(interior, $"Seat{seatIndex}", new Vector3(x, FloorY, z), seatMat, interiorMat);
                refs.Seats[seatIndex] = seat.transform;
                seatIndex++;
            }
        }

        // Буфет (кухня) справа спереди.
        var galleyGo = new GameObject("Galley");
        galleyGo.transform.SetParent(interior, false);
        galleyGo.transform.localPosition = new Vector3(0.95f, FloorY, 4.6f);
        ProceduralAssets.Box(galleyGo.transform, "Box", Vector3.zero, Vector3.zero,
            new Vector3(0.7f, 1.5f, 0.8f), metalMat);
        ProceduralAssets.Box(galleyGo.transform, "Dispenser", new Vector3(0f, 0.85f, 0.1f), Vector3.zero,
            new Vector3(0.3f, 0.3f, 0.3f), ProceduralAssets.Mat("Water", new Color(0.3f, 0.7f, 0.95f), lit: false));
        refs.Galley = galleyGo.AddComponent<Galley>();

        // Грузовой отсек — ящики в хвосте.
        var cargo = new GameObject("Cargo");
        cargo.transform.SetParent(interior, false);
        cargo.transform.localPosition = new Vector3(0f, FloorY, -5.3f);
        var crateMat = ProceduralAssets.Mat("Crate", new Color(0.55f, 0.4f, 0.25f));
        for (int i = 0; i < 6; i++)
        {
            ProceduralAssets.Box(cargo.transform, $"Crate{i}",
                new Vector3(-0.8f + (i % 3) * 0.8f, 0.25f + (i / 3) * 0.5f, -0.2f + (i / 3) * 0.4f),
                new Vector3(0f, (i * 37f) % 30f, 0f), new Vector3(0.7f, 0.45f, 0.7f), crateMat);
        }
        refs.CargoArea = cargo.transform;

        // ============ КАБИНА ПИЛОТОВ ============
        // Приборная панель.
        ProceduralAssets.Box(interior, "Panel", new Vector3(0f, FloorY + 0.95f, 6.35f), new Vector3(-18f, 0f, 0f),
            new Vector3(1.9f, 0.7f, 0.5f), darkMat);
        // «Приборы» — светящиеся кругляши.
        var gaugeMat = ProceduralAssets.Mat("Gauge", new Color(0.25f, 0.95f, 0.55f), lit: false);
        for (int i = 0; i < 4; i++)
        {
            ProceduralAssets.Quad(interior, $"Gauge{i}", new Vector3(-0.6f + i * 0.4f, FloorY + 1.05f, 6.12f),
                new Vector3(-18f, 0f, 0f), new Vector2(0.22f, 0.22f), gaugeMat);
        }
        // Лобовое стекло.
        ProceduralAssets.Quad(ext, "Windshield", new Vector3(0f, 0.55f, 6.75f), new Vector3(-25f, 0f, 0f),
            new Vector2(1.9f, 0.9f), glassMat);

        // Кресла пилотов (якоря).
        refs.PilotSeat = BuildSeat(interior, "PilotSeat", new Vector3(-0.42f, FloorY, 5.3f), seatMat, interiorMat).transform;
        refs.CopilotSeat = BuildSeat(interior, "CopilotSeat", new Vector3(0.42f, FloorY, 5.3f), seatMat, interiorMat).transform;

        // Штурвал.
        var yokeGo = new GameObject("SteeringWheel");
        yokeGo.transform.SetParent(interior, false);
        yokeGo.transform.localPosition = new Vector3(-0.42f, FloorY + 0.55f, 5.95f);
        refs.Yoke = yokeGo.AddComponent<SteeringWheel>();
        refs.Yoke.BuildVisual();

        // ============ ДВЕРИ ============
        // Левая передняя дверь (главная).
        var doorGo = new GameObject("MainDoor");
        doorGo.transform.SetParent(interior, false);
        doorGo.transform.localPosition = new Vector3(-1.28f, FloorY + 0.95f, 3.85f);
        refs.MainDoor = doorGo.AddComponent<DoorInteraction>();
        refs.MainDoor.isMainDoor = true;
        refs.MainDoor.BuildVisual();

        // Правый люк выхода на крыло.
        var hatchGo = new GameObject("WingHatch");
        hatchGo.transform.SetParent(interior, false);
        hatchGo.transform.localPosition = new Vector3(1.28f, FloorY + 0.95f, 0.7f);
        refs.WingHatch = hatchGo.AddComponent<WingHatch>();
        refs.WingHatch.BuildVisual();

        // Поверхность хождения по крылу (невидимый коллайдер).
        var walkGo = new GameObject("WingWalk");
        walkGo.transform.SetParent(root, false);
        var walkCollider = walkGo.AddComponent<BoxCollider>();
        walkCollider.center = new Vector3(4.4f, 0.42f, 0.7f);
        walkCollider.size = new Vector3(7.0f, 0.1f, 2.3f);

        // Якорь шланга заправки на правом крыле.
        var hose = new GameObject("HoseAnchor");
        hose.transform.SetParent(root, false);
        hose.transform.localPosition = new Vector3(5.8f, 0.5f, 0.7f);
        refs.HoseAnchor = hose;

        // Точка возврата в салон (у корня крыла).
        var entryGo = new GameObject("WingEntry");
        entryGo.transform.SetParent(root, false);
        entryGo.transform.localPosition = new Vector3(1.9f, 0.6f, 0.7f);
        var entry = entryGo.AddComponent<WingEntry>();
        entry.maxDistance = 3.2f;

        return refs;
    }

    /// <summary>Гондола двигателя с винтом.</summary>
    private static Transform BuildEngine(Transform parent, Vector3 pos, Material metal, Material dark,
        out Transform prop)
    {
        var eng = new GameObject("Engine");
        eng.transform.SetParent(parent, false);
        eng.transform.localPosition = pos;
        ProceduralAssets.Cylinder(eng.transform, "Nacelle", Vector3.zero, new Vector3(90f, 0f, 0f),
            0.5f, 1.9f, metal);
        ProceduralAssets.Box(eng.transform, "Pylon", new Vector3(0f, 0.42f, -0.2f), Vector3.zero,
            new Vector3(0.24f, 0.5f, 0.8f), metal);

        // Винт: ступица + две лопасти (вращается вокруг Z).
        prop = new GameObject("Prop").transform;
        prop.SetParent(eng.transform, false);
        prop.localPosition = new Vector3(0f, 0f, 1.05f);
        ProceduralAssets.Cylinder(prop, "Hub", Vector3.zero, new Vector3(90f, 0f, 0f), 0.12f, 0.2f, dark);
        ProceduralAssets.Box(prop, "Blade1", Vector3.zero, Vector3.zero, new Vector3(0.16f, 3.3f, 0.05f), dark);
        ProceduralAssets.Box(prop, "Blade2", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.16f, 3.3f, 0.05f), dark);
        return eng.transform;
    }

    /// <summary>Кресло: основание + спинка; якорь посадки — сам объект.</summary>
    private static GameObject BuildSeat(Transform parent, string name, Vector3 pos, Material seatMat, Material interiorMat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        ProceduralAssets.Box(go.transform, "Base", new Vector3(0f, 0.22f, 0f), Vector3.zero,
            new Vector3(0.46f, 0.16f, 0.46f), seatMat);
        ProceduralAssets.Box(go.transform, "Back", new Vector3(0f, 0.62f, 0.2f), new Vector3(-8f, 0f, 0f),
            new Vector3(0.46f, 0.72f, 0.1f), seatMat);
        ProceduralAssets.Box(go.transform, "Headrest", new Vector3(0f, 1.02f, 0.22f), Vector3.zero,
            new Vector3(0.3f, 0.16f, 0.1f), seatMat);
        return go;
    }
}

/// <summary>Буфет: взаимодействие «взять стакан воды».</summary>
public class Galley : Interactable
{
    public override string GetPrompt(PlayerController player)
        => player != null && player.hasWater ? "У вас уже есть стакан воды" : "Взять стакан воды (E)";

    public override void Interact(PlayerController player)
    {
        if (player == null) return;
        player.SetHasWater(true);
    }
}
