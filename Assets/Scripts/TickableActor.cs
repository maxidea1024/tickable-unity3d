using UnityEngine;
using UnityEngine.UI;

public class TickableActor : MonoBehaviour
{
    void Awake()
    {
        if (tickable == null)
        {
            tickable = GetComponent<Tickable>();
        }

        tickable.tick_ += (uint tickCount) =>
        {
            tickCounter += tickCount;

            if (logTick)
            {
                Debug.Log(string.Format("Tick({0}, {1})", tickCount, tickCounter));
            }

            debugText.text = string.Format("TICKS: {0}, TICK_RATE: {1}, MS_PER_TICK: {2}", tickCounter, tickable.tickRate_, tickable.msPerTick_);
        };

        tickable.interpolateTick_ += (float percentage) =>
        {
            if (logInterpolateTick)
            {
                Debug.Log(string.Format("InterpolateTick({0})", percentage));
            }
        };

        tickable.advanceTime_ += (float deltaSeconds) =>
        {
            if (logAdvanceTime)
            {
                Debug.Log(string.Format("AdvanceTime({0})", deltaSeconds));
            }
        };
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            tickable.isTickingEnabled_ = !tickable.isTickingEnabled_;
        }
    }


    //
    // Member variables
    //

    //[HideInInspector]
    public Tickable tickable;

    public uint tickCounter = 0;

    public bool logTick = true;
    public bool logInterpolateTick = false;
    public bool logAdvanceTime = false;

    public Text debugText;
}
