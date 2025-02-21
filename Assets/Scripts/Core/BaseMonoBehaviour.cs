using UnityEngine;

/// <summary>
/// Base class for MonoBehaviour scripts that interact with the New Update System.
/// It provides methods for subscribing and unsubscribing to update events
/// and supports custom OnUpdate, OnFixedUpdate, and OnLateUpdate methods.
/// </summary>
public class BaseMonoBehaviour : MonoBehaviour
{
    #region Fields

    /// <summary>
    /// Indicates whether this object is paused.
    /// </summary>
    protected bool _isPaused = false;

    #endregion

    #region Properties

    /// <summary>
    /// Controls whether this object is individually paused.
    /// </summary>
    public bool IsIndividuallyPaused
    {
        get => _isPaused;
        set => _isPaused = value;
    }

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Subscribes to the UpdateManager when the script starts.
    /// </summary>
    protected virtual void Start() => UpdateManager.Subscribe(this);

    /// <summary>
    /// Called when the object is destroyed. Handles unsubscription from UpdateManager.
    /// </summary>
    protected virtual void OnDestroy() => UnsubscribeFromUpdates();

    /// <summary>
    /// Called when the object is disabled. Handles unsubscription from UpdateManager.
    /// </summary>
    protected virtual void OnDisable() => UnsubscribeFromUpdates();

    #endregion

    #region UpdateManager Methods

    /// <summary>
    /// Called every frame during the Update phase. Override to define custom behavior.
    /// </summary>
    public virtual void OnUpdate() { }

    /// <summary>
    /// Called during the FixedUpdate phase. Override to define fixed-step physics logic.
    /// </summary>
    public virtual void OnFixedUpdate() { }

    /// <summary>
    /// Called during the LateUpdate phase. Override to define post-frame logic.
    /// </summary>
    public virtual void OnLateUpdate() { }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Unsubscribes the object from the UpdateManager to avoid dangling references.
    /// </summary>
    private void UnsubscribeFromUpdates() => UpdateManager.Unsubscribe(this);

    #endregion
}