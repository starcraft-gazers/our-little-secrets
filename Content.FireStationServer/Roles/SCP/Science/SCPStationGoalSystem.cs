using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Fax;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.FireStationServer.Roles.SCP.Science;

//Быстренько запилил, буду переписывать в нормальный вид
public sealed class SCPStationGoalSystem : EntitySystem
{
    [Dependency] private readonly FaxSystem _faxSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    private string LastTask = default!;
    private float Elapsed = -1f;
    private float NextTaskDelay = -1f;

    private readonly List<string> HardcodedTasks = new List<string>
    {
        "Доставить SCP-173 на станцию и проверить на воздействие различными частицами (М.А.К.А.К.)",
        "Доставить SCP-173 на станцию к источнику радиации на 2 минут",
        "Дождаться наличие преступника в пермабриге и доставить его в камеру к SCP-173",
        "Доставить SCP-049 на станцию и попросить его вылечить двух раненых или больных",
        "Доставить SCP-049 на станцию для прогулки. 10-20 минут",
        "Доставить Странное мыло на станцию и выпустить на самостоятельную прогулку 30 минут",
        "Вернуть странное мыло в отдел SCP.",
    };
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);
    }

    private void OnRoundEnded(RoundEndedEvent ev)
    {
        Elapsed = -1;
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        Elapsed = 0f;
        NextTaskDelay = 120f;
    }

    public override void Update(float frameTime)
    {
        if (Elapsed == -1)
            return;

        Elapsed += frameTime;

        if (Elapsed >= NextTaskDelay)
        {
            PrintNextTask();
            Elapsed = 0f;
            NextTaskDelay = _robustRandom.NextFloat(420, 600);
        }
    }

    private void PrintNextTask()
    {
        var filtered = HardcodedTasks.Where(task => task != LastTask).ToList();
        if (filtered == null)
            return;

        _robustRandom.Shuffle(filtered);

        var randomTask = _robustRandom.Pick(filtered);
        var faxes = EntityQuery<FaxMachineComponent>();

        foreach (var fax in faxes)
        {
            if (!fax.ReceiveSCPGoal)
                continue;

            var printout = new FaxPrintout(
                randomTask,
                "Задание отделу SCP",
                null,
                "paper_stamp-cent",
                new() { Loc.GetString("stamp-component-stamped-name-centcom") });
            _faxSystem.Receive(fax.Owner, printout, null, fax);
        }
    }
}
