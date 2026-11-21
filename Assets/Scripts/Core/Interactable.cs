// Interactable.cs — базовый класс всего, с чем можно взаимодействовать клавишей E / F.
// Пассажиры, штурвал, дверь, буфет с водой, шланг заправки и т.д. наследуются от него.

using System.Collections.Generic;
using UnityEngine;

/// <summary>Базовый интерактивный объект. PlayerController ищет их рейкастом из центра экрана.</summary>
public abstract class Interactable : MonoBehaviour
{
    /// <summary>Глобальный реестр всех интерактивов сцены.</summary>
    public static readonly List<Interactable> All = new List<Interactable>();

    [Header("Общие параметры взаимодействия")]
    /// <summary>Дальность взаимодействия в метрах.</summary>
    public float maxDistance = 3.0f;

    protected virtual void OnEnable() { All.Add(this); }
    protected virtual void OnDisable() { All.Remove(this); }

    /// <summary>Текст подсказки (показывается по центру экрана). Null/пусто — не показывать.</summary>
    public virtual string GetPrompt(PlayerController player) => null;

    /// <summary>Нажатие E. По умолчанию — ничего.</summary>
    public virtual void Interact(PlayerController player) { }

    /// <summary>Нажатие F (вторичное действие, например «напоить водой»).</summary>
    public virtual void SecondaryInteract(PlayerController player) { }

    /// <summary>true — интеракт работает «с удержанием» (полоска прогресса), например захват пассажира.</summary>
    public virtual bool IsHoldInteract => false;

    /// <summary>Сколько секунд удерживать E (для IsHoldInteract).</summary>
    public virtual float HoldDuration => 0.8f;

    /// <summary>Можно ли сейчас взаимодействовать (иначе подсказка не показывается).</summary>
    public virtual bool CanInteract(PlayerController player) => enabled && gameObject.activeInHierarchy;
}
