//TODO 베이스 시간을 기점으로 오차(rounding error) 누적 피하기
//TODO enabled/disabled시점에 대한 처리
//TODO time-scale에 대한 대응
//TODO tick rate를 0으로 해서 disable하는 용도로 쓰고 싶을 경우에는 어떻게?
//TODO 실행 주기를 그래프로 보여주어서 튀는 현상을 확인하도록 하자.

using UnityEngine;

public class Tickable : MonoBehaviour
{
    public const int MIN_TICK_RATE = 1;
    public const int MAX_TICK_RATE = 250;

    public delegate void FixedTickDelegate(uint tickCount);
    public delegate void InterpolateFixedTickDelegate(float percentage);
    public delegate void AdvanceTimeDelegate(float deltaSeconds);

    public event FixedTickDelegate tick_;
    public event InterpolateFixedTickDelegate interpolateTick_;
    public event AdvanceTimeDelegate advanceTime_;

    //AdvanceTime자체도 별도로 마스킹할수 있나?
    //FixedTick 같은 경우 disable -> enable 시에
    public bool isTickingEnabled_ = true;

    public bool allowTickCollapsing = false;

    /**
     * 실행도중 중간에 변경했을 경우에 매끄러운 처리가 필요함.
     * 이 부분에 대한 고민을 해봐야할듯.
     *
     * 중간에 변경하지 않으면 되는건가??
     * 중간에 변경하는건 옳지 않은것 같다.
     */
    public int tickRate_ = 30;
    public uint msPerTick_ = 1000 / 30;

    public enum DeltaTimeType
    {
        Scaled,
        Unscaled,
    }
    public DeltaTimeType deltaTimeType_ = DeltaTimeType.Scaled;

    private uint lastTickMs_ = 0;
    private uint lastTimeMs_ = 0;

    private void Update()
    {
        uint deltaMs;

        if (deltaTimeType_ == DeltaTimeType.Scaled)
        {
            deltaMs = (uint)(Time.deltaTime * 1000f);
        }
        else
        {
            deltaMs = (uint)(Time.unscaledDeltaTime * 1000f);
        }

        if (deltaMs > 0)
        {
            AdvanceTime(deltaMs);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            --tickRate_;
            if (tickRate_ < MIN_TICK_RATE)
            {
                tickRate_ = MIN_TICK_RATE;
            }
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ++tickRate_;
            if (tickRate_ > MAX_TICK_RATE)
            {
                tickRate_ = MAX_TICK_RATE;
            }
        }
    }

    /**
     * 보간을 하려면, 다음 프레임이 구해져있어야하지 않을런지?
     * 그냥 velocity만큼 전진시키면 되나??
     */
    private void AdvanceTime(uint deltaMs)
    {
        // 이 값은 이대로 유지를하고..
        uint targetTimeMs = lastTimeMs_ + deltaMs;

        // 고정틱 하나당 millisecond를 계산함.
        // enabled 상태에서만 변경해주는게 좋을듯..
        // disabled된 상태에서는 변경되지 않는게 혼선을 덜 줄 수 있을듯..
        if (isTickingEnabled_)
        {
            if (tickRate_ <= 0)
            {
                // 최저 tick-rate로 제한하지 않고, <=0이면 diabled 의미로 사용해야할까??
                msPerTick_ = 1000 / MIN_TICK_RATE;
            }
            else
            {
                msPerTick_ = (uint)(1000 / tickRate_);
            }
        }

        // 고정틱 처리
        if (isTickingEnabled_ && tick_ != null)
        {
            uint targetTickMs = (targetTimeMs + msPerTick_ - 1);
            uint fraction = targetTickMs % msPerTick_;
            if (fraction != 0)
            {
                targetTickMs -= fraction;
            }

            // lastTickMs=41000
            // targetTickMs=40800
            // 아니 어째서 targetTickMs가 더 커질수 있을까??
            // 역전이 되는 현상을 어찌 해결할 수 있을까?
            // 변경시에 끊김없이 이어가게 하려면 어떻게 해야할까?
            //uint tickCount = (targetTickMs - lastTickMs_) / msPerTick_;
            uint tickCount;
            if (targetTickMs > lastTickMs_)
            {
                tickCount = (targetTickMs - lastTickMs_) / msPerTick_;
            }
            else
            {
                //역전이 일어날 경우에는 엄청난 수치로 계산이 되므로 일단은 이렇게 처리.
                tickCount = 0;
            }

            if (tickCount != 0)
            {
                if (tickCount >= 2)
                {
                    Debug.Log(string.Format("multiple ticked: tickCount={0}, tickRate={1}, msPerTick_={2}, targetTickMs={3}, lastTickMs={4}", tickCount, tickRate_, msPerTick_, targetTickMs, lastTickMs_));
                }

                // FIXME
                // 한참동안 disabled 되었다가 enable되면 대량의 호출이 발생하게됨.

                // 틱이 중첩이 된 경우에는 다른 Tick에서도 시간은 같을 수가 있음.

                if (tickCount > 1 && allowTickCollapsing)
                {
                    //TODO 이렇게 하는 것보다는 맨마지막 틱위치에서 호출될 수 있도록 하는게 좋을듯...
                    //for (; lastTickMs_ < targetTickMs; lastTickMs_ += msPerTick_);
                    //tick_(tickCount);

                    //TODO friction으로 인해서 오차가 반영되지는 않는지?

                    // 리니어하게 처리할 수 있는 구조가 아니라면,
                    // 오류를 유발하게 됨.

                    lastTickMs_ += msPerTick_ * (tickCount - 1); // 마지막 틱 위치에서의 타임 위치 설정
                                                                 // fixed current time액세스를 할 경우를 위해서..
                    tick_(tickCount);
                    lastTickMs_ += msPerTick_;
                }
                else
                {
                    for (; lastTickMs_ < targetTickMs; lastTickMs_ += msPerTick_)
                    {
                        tick_(1);
                    }
                }
            }
            else
            {
                lastTickMs_ = targetTickMs;
            }
        }

        if (isTickingEnabled_ && interpolateTick_ != null)
        {
            uint fraction = targetTimeMs % msPerTick_;
            float percentage = (float)fraction / (float)msPerTick_;

            interpolateTick_(percentage);
        }

        //TODO 여기에서 틱당 msec값을 재계산을 해주는게 좋을듯...

        // advanceTime은 항상 호출됨.
        if (advanceTime_ != null)
        {
            float deltaSeconds = (float)deltaMs / 1000f;

            advanceTime_(deltaSeconds);
        }

        // Advance time position.
        lastTimeMs_ += deltaMs;
    }
}
