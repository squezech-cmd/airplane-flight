// RadioAnnouncer.cs — объявления капитана: очередь, субтитры на HUD, радио-писк.
// Чёрный юмор «худшей авиакомпании мира». 18+, развлекательные цели.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Типы объявлений (ключи к репликам).</summary>
public enum AnnouncementType
{
    Welcome,        // приветствие после взлёта
    Turbulence,     // турбулентность
    BirdStrike,     // птица в двигатель
    EngineFire,     // пожар двигателя
    LowFuel,        // мало топлива
    RefuelStart,    // летит заправщик
    RefuelDone,     // бак полон
    PassengerOut,   // пассажир покинул борт
    Fight,          // драка в салоне
    Landing,        // заход на посадку
    Crash,          // крушение
    Stall,          // сваливание
    RandomJoke,     // случайная шутка
}

/// <summary>Радио-объявления с субтитрами.</summary>
public class RadioAnnouncer : MonoBehaviour
{
    public static RadioAnnouncer Instance { get; set; }

    private static readonly Dictionary<AnnouncementType, string[]> Lines = new Dictionary<AnnouncementType, string[]>
    {
        [AnnouncementType.Welcome] = new[]
        {
            "Дорогие пассажиры! Приветствуем вас на борту. Экипаж — это я, и я делаю всё.",
            "Дорогие пассажиры! Высота полёта — как получится, питание — по остаточному принципу.",
            "Уважаемые пассажиры! Пристегните ремни. Ремни в количестве двух уже пристёгнуты мной к креслам."
        },
        [AnnouncementType.Turbulence] = new[]
        {
            "Дорогие пассажиры! Попадаем в зону турбулентности. Напоминаю: напитки — платно, отстировка — за ваш счёт.",
            "Капитан просил передать: сейчас немного тряхнёт. Или много. Он сам ещё не знает.",
            "Внимание! То, что вы сейчас почувствуете, аэродинамики называют «воздушная яма». Юристы — «форс-мажором»."
        },
        [AnnouncementType.BirdStrike] = new[]
        {
            "Дорогие пассажиры! Правый двигатель съел птичку. Не переживайте: он плотно завтракал.",
            "У нас небольшая ornitологическая проблема. Двигатель цел. Почти.",
            "Птица приняла наш двигатель за маму. Двигатель не оценил."
        },
        [AnnouncementType.EngineFire] = new[]
        {
            "Дорогие пассажиры! Двигатель горит. Просьба не поддаваться панике раньше, чем я.",
            "Небольшое возгорание двигателя — это не катастрофа. Это бесплатное шоу.",
            "Огонь в двигателе потушим. Главное, чтобы он не спросил добавки."
        },
        [AnnouncementType.LowFuel] = new[]
        {
            "Дорогие пассажиры! Похоже, мы недозаправились. Зато летим легче.",
            "Топлива осталось на дне бака. Экономический отдел передаёт вам привет.",
            "Индикатор топлива мигает. По инструкции это «информативная подсветка»."
        },
        [AnnouncementType.RefuelStart] = new[]
        {
            "Дорогие пассажиры! К нам летит самолёт-заправщик. Кто с крыла — тому премия.",
            "Сейчас наш бортстряпчий выйдет на крыло и покормит самолёт с ложечки."
        },
        [AnnouncementType.RefuelDone] = new[]
        {
            "Бак полон! Обнимаю заправщика и иду пить кофе.",
            "Заправка завершена. Спасибо, что не смотрели."
        },
        [AnnouncementType.PassengerOut] = new[]
        {
            "Дорогие пассажиры! Один из вас покинул борт досрочно. Просьба занять его место у окна.",
            "Пассажир Иванов высадился по собственному желанию. Желание было моё.",
            "Минус один пассажир, минус одна проблема. Аэродинамика улучшилась."
        },
        [AnnouncementType.Fight] = new[]
        {
            "Дорогие пассажиры! В третьем ряду проходят тэвтэчные соревнования. Ставки принимает стюард.",
            "Просьба прекратить драку. Или продолжать, но с выражением."
        },
        [AnnouncementType.Landing] = new[]
        {
            "Дорогие пассажиры! Мы заходим на посадку. Пожалуйста, примите вертикальное положение и сожаления.",
            "Начинаем снижение. Если увидите землю из окна — это нормально. Если из иллюминатора в кабине — тоже."
        },
        [AnnouncementType.Crash] = new[]
        {
            "Дорогие пассажиры! Произошла небольшая авиакатастрофа. Спасибо, что летели с нами.",
            "Вот и всё. Главное — долетели. Вопрос — куда."
        },
        [AnnouncementType.Stall] = new[]
        {
            "Дорогие пассажиры! Самолёт устал и хочет отдохнуть. Уговорим его лететь дальше.",
            "Сваливание! Это когда самолёт и гравитация наконец нашли общий язык."
        },
        [AnnouncementType.RandomJoke] = new[]
        {
            "Дорогие пассажиры! Температура за бортом минус пятьдесят. В салоне — тоже, экономия.",
            "Напоминаю: курение на борту запрещено. Especially в туалете. Особенно вы, третий ряд.",
            "Наш самолёт эксплуатируется с прошлого века. Как и мои шутки.",
            "Просьба не трясти самолёт изнутри — он и сам неплохо справляется.",
            "Дамы и господа! В случае разгерметизации маски не предусмотрены. Зато предусмотрена вентиляция.",
            "Обед будет подан, когда стюард вспомнит, где он его оставил.",
            "Осторожно, двери закрываются! Тех, кто не успел, поздравляем — вы не полетели."
        },
    };

    private readonly Queue<AnnouncementType> _queue = new Queue<AnnouncementType>();
    private float _cooldown;
    private float _jokeTimer = 90f;
    private System.Random _rng = new System.Random(2026);

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        _cooldown -= Time.deltaTime;
        _jokeTimer -= Time.deltaTime;

        // Случайные шутки в полёте.
        var session = GameSession.Instance;
        if (session != null && session.Phase.Value == (int)GamePhase.Flight && _jokeTimer <= 0f)
        {
            _jokeTimer = 70f + _rng.Next(0, 60);
            Enqueue(AnnouncementType.RandomJoke);
        }

        if (_cooldown > 0f || _queue.Count == 0) return;
        var type = _queue.Dequeue();
        _cooldown = 9f;
        StartCoroutine(PlayAnnouncement(type));
    }

    /// <summary>Добавить объявление в очередь (вызывается на любом пире при получении события).</summary>
    public void Enqueue(AnnouncementType type)
    {
        if (_queue.Count < 3) _queue.Enqueue(type);
    }

    private IEnumerator PlayAnnouncement(AnnouncementType type)
    {
        if (HUD.Instance == null) yield break;

        var lines = Lines.TryGetValue(type, out var arr) ? arr : Lines[AnnouncementType.RandomJoke];
        string text = lines[_rng.Next(lines.Length)];

        // Писк «дин-дин» перед объявлением.
        var beepSrc = new GameObject("RadioBeep").AddComponent<AudioSource>();
        beepSrc.clip = ProceduralAudio.Beep;
        beepSrc.volume = 0.6f * GameSettings.Current.sfxVolume;
        beepSrc.spatialBlend = 0f;
        beepSrc.Play();
        Destroy(beepSrc.gameObject, 1f);
        yield return new WaitForSeconds(0.6f);

        HUD.Instance.Subtitle("КАПИТАН: " + text, 7f);
    }
}
