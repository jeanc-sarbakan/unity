using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TestAwaitable : MonoBehaviour
{
    [SerializeField] private Button _startProcessButton;
    [SerializeField] private Button _loadSceneButton;
    [SerializeField] private Button _waitCancelButton;

    private CancellationTokenSource _cancellationTokenSource;
    
    private void Start()
    {
        _startProcessButton.onClick.AddListener(StartAsyncProcess);
        _loadSceneButton.onClick.AddListener(LoadSceneExample);
        
        var colors = _waitCancelButton.colors;
        colors.normalColor = Color.indianRed;
        colors.highlightedColor = Color.indianRed;
        _waitCancelButton.colors = colors;
        
        _waitCancelButton.onClick.AddListener(OnWaitCancelButtonClicked);
    }

    private void OnWaitCancelButtonClicked()
    {
        if (_cancellationTokenSource != null)
        {
            CancelWaitOperation();
        }
        else
        {
            WaitTenSecondsAsync();
        }
    }

    private void CancelWaitOperation()
    {
        _cancellationTokenSource?.Cancel();
        Debug.Log("Cancel requested on 10 second wait");
    }

    // An async void method is often used for event handlers or Unity lifecycle methods (like Start)
    private async void StartAsyncProcess()
    {
        Debug.Log("Process Started");
        
        try 
        {
            await PerformComplexTaskAsync();
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Task was cancelled.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error: {e.Message}");
        }

        Debug.Log("Process Finished");
    }

    private async Awaitable PerformComplexTaskAsync()
    {
        // 1. Wait for the next frame (replacement for yield return null)
        await Awaitable.NextFrameAsync();
        Debug.Log("Waited one frame");

        // 2. Wait for a specific amount of time (replacement for WaitForSeconds)
        await Awaitable.WaitForSecondsAsync(1.5f);
        Debug.Log("Waited 1.5 seconds");

        // 3. Switch to a background thread for heavy calculation
        await Awaitable.BackgroundThreadAsync();
        
        // Heavy calculation happens off the main thread
        double result = 0;
        for (int i = 0; i < 1_000_000; i++)
        {
            result += Math.Sqrt(i);
        }
        
        // 4. Switch back to the Main Thread to update Unity objects
        await Awaitable.MainThreadAsync();
        
        transform.position += Vector3.up;
        Debug.Log($"Calculation done: {result}. Moved object on main thread.");
        
        // 5. Wait for a fixed update (physics)
        await Awaitable.FixedUpdateAsync();
        Debug.Log("Inside FixedUpdate");

        // 6. Wait for end of frame (after rendering, useful for screen capture)
        await Awaitable.EndOfFrameAsync();
        Debug.Log("End of frame reached");
        
        // Create an awaitable task (wait for 1 second)
        var myAwaitable = Awaitable.WaitForSecondsAsync(1f);

        // 7. Check IsCompleted property
        // This allows you to check if the operation has finished without awaiting it immediately
        Debug.Log($"IsCompleted before await: {myAwaitable.IsCompleted}");
        await myAwaitable;
        Debug.Log($"IsCompleted after await: {myAwaitable.IsCompleted}");
    }

    private async void LoadSceneExample()
    {
        Debug.Log("Starting Scene Load...");
        try
        {
            
            // 8. Example of awaiting an AsyncOperation (like loading a scene or resources)
            // This converts a legacy AsyncOperation into an Awaitable.
            // Note: Ensure "TestScene" exists in Build Settings or change the name.
            var asyncOp = SceneManager.LoadSceneAsync("TestScene", LoadSceneMode.Additive);
            if (asyncOp != null)
            {
                await Awaitable.FromAsyncOperation(asyncOp);
                Debug.Log("Scene Loaded");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load scene: {e.Message}");
        }
    }

    private async void WaitTenSecondsAsync()
    {
        // 9. Example of using Awaitable.Cancel
        // This demonstrates waiting for 10 seconds and allowing cancellation via CancellationToken
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Set button color to green when running
        if (_waitCancelButton != null)
        {
            var colors = _waitCancelButton.colors;
            colors.normalColor = Color.mediumSpringGreen;
            colors.highlightedColor = Color.mediumSpringGreen;
            colors.selectedColor = Color.mediumSpringGreen;
            _waitCancelButton.colors = colors;
        }
        
        Debug.Log("Starting 10 second wait - click button to cancel");
        
        try
        {
            await Awaitable.WaitForSecondsAsync(10f, _cancellationTokenSource.Token);
            Debug.Log("Successfully waited 10 seconds!");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("10 second wait was cancelled!");
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            
            // Set button color back to red when stopped
            if (_waitCancelButton != null)
            {
                var colors = _waitCancelButton.colors;
                colors.normalColor = Color.indianRed;
                colors.highlightedColor = Color.indianRed;
                colors.selectedColor = Color.indianRed;
                _waitCancelButton.colors = colors;
            }
        }
    }
    
    // Example of running an Awaitable and destroying the token when the object is destroyed
    private async void OnEnable()
    {
        // Pass the destroyCancellationToken to stop the awaitable if the GameObject is destroyed
        await WaitUntilDestroyed(destroyCancellationToken);
    }

    private async Awaitable WaitUntilDestroyed(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                //Debug.Log("Still alive...");
                await Awaitable.WaitForSecondsAsync(1f, token);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Stopped because object was disabled or destroyed.");
        }
    }
}
