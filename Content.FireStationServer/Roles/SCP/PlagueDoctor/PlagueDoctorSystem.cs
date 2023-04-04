using System.Linq;
using Content.Server.Disease.Components;
using Content.Server.Humanoid;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Shared.Actions;
using Content.Shared.Humanoid;
using Content.Shared.Rejuvenate;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.FireStationServer.Roles.SCP.PlagueDoctor;

public sealed class PlagueDoctorSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _sharedHuApp = default!;

    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<PlagueDoctorComponent, ComponentInit>(OnPlagueDoctorInit);
        SubscribeLocalEvent<PlagueDoctorComponent, ComponentRemove>(OnPlagueDoctorRemove);
        SubscribeLocalEvent<PlagueDoctorComponent, PlagueDoctorHealEvent>(OnHealEvent);
        SubscribeLocalEvent<PlagueDoctorComponent, PlagueDoctorZombieEvent>(OnMakeZombieEvent);
    }

    private void OnPlagueDoctorInit(EntityUid uid, PlagueDoctorComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, component.HealAction, uid);
        _actionsSystem.AddAction(uid, component.MakeZombieAction, uid);
    }

    private void OnPlagueDoctorRemove(EntityUid uid, PlagueDoctorComponent component, ComponentRemove args)
    {
        _actionsSystem.RemoveAction(uid, component.HealAction);
        _actionsSystem.RemoveAction(uid, component.MakeZombieAction);
    }

    private void OnHealEvent(EntityUid uid, PlagueDoctorComponent component, PlagueDoctorHealEvent args)
    {
        args.Handled = true;
        var target = args.Target;

        if (HasComp<HumanoidAppearanceComponent>(target))
        {
            RaiseLocalEvent(target, new RejuvenateEvent());
            _popupSystem.PopupEntity("Чумной доктор исцелил вас!", target);
        }
    }

    private void OnMakeZombieEvent(EntityUid uid, PlagueDoctorComponent component, PlagueDoctorZombieEvent args)
    {
        args.Handled = true;
        var target = args.Target;

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            _popupSystem.PopupEntity("Мы можете превратить в своих спутников только людей", uid);
            return;
        }

        var zombies = EntityQuery<PlagueDoctorZombieComponent>()?.Where(p => p.ZombieOwner == uid);
        if (zombies != null && zombies.Count() == 2)
        {
            _popupSystem.PopupEntity("Вы не можете иметь больше двух спутников", uid);
            return;
        }

        RemComp<DiseaseCarrierComponent>(target);
        RemComp<ThirstComponent>(target);

        EnsureComp<ReplacementAccentComponent>(target).Accent = "mute";

        var zombiecomp = EnsureComp<PlagueDoctorZombieComponent>(target);
        zombiecomp.ZombieOwner = uid;

        if (TryComp<HumanoidAppearanceComponent>(target, out var huApComp))
        {

            _sharedHuApp.SetSkinColor(target, zombiecomp.SkinColor, humanoid: huApComp);
            _sharedHuApp.SetBaseLayerColor(target, HumanoidVisualLayers.Eyes, zombiecomp.EyeColor, humanoid: huApComp);

            _sharedHuApp.SetBaseLayerId(target, HumanoidVisualLayers.Tail, zombiecomp.BaseLayerExternal, humanoid: huApComp);
            _sharedHuApp.SetBaseLayerId(target, HumanoidVisualLayers.HeadSide, zombiecomp.BaseLayerExternal, humanoid: huApComp);
            _sharedHuApp.SetBaseLayerId(target, HumanoidVisualLayers.HeadTop, zombiecomp.BaseLayerExternal, humanoid: huApComp);
            _sharedHuApp.SetBaseLayerId(target, HumanoidVisualLayers.Snout, zombiecomp.BaseLayerExternal, humanoid: huApComp);
        }


        var meta = MetaData(target);
        meta.EntityName = $"Спутник чумного доктора {meta.EntityName}";

        _popupSystem.PopupEntity("Чумной доктор превратил вас в своего спутника... Теперь вы подчиняетесь ему", target);
    }
}
