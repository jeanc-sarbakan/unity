using System;
using UnityEngine;
using UnityEngine.UI;

public class TestAwaitable : MonoBehaviour
{
    [SerializeField] private Button _startProcessButton;

    private void Start()
    {
        _startProcessButton.onClick.AddListener(StartAsyncProcess);

        PerformComplexTaskAsync();
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
    }
    
    // Example of running an Awaitable and destroying the token when the object is destroyed
    private async void OnEnable()
    {
        // Pass the destroyCancellationToken to stop the awaitable if the GameObject is destroyed
        await WaitUntilDestroyed(destroyCancellationToken);
    }

    private async Awaitable WaitUntilDestroyed(System.Threading.CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                Debug.Log("Still alive...");
                await Awaitable.WaitForSecondsAsync(1f, token);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Stopped because object was disabled or destroyed.");
        }
    }
}
