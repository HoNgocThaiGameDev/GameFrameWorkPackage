using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NebulaSoft;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class CoinSafeProgressTests
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private const string ConflictDialogPath =
        "Assets/Project Files/Game/Features/Profile/Prefabs/SaveProgressFoundDialog.prefab";

    [TearDown]
    public void TearDown()
    {
        CoinSafeProgress.EndFacebookAuthTransition();
    }

    [Test]
    public void NewSave_MigratesToGuestWithZeroAmount()
    {
        CoinSafeSave save = new CoinSafeSave();

        InvokeEnsureMigrated(save);

        CoinSafeAccountEntry guest = GetGuestEntry(save);
        Assert.That(save.SchemaVersion, Is.EqualTo(2));
        Assert.That(save.ActiveOwnerKey, Is.EqualTo(guest.OwnerKey));
        Assert.That(guest.Amount, Is.Zero);
    }

    [Test]
    public void LegacyAmount_MigratesOnceIntoGuestBucket()
    {
        CoinSafeSave save = new CoinSafeSave
        {
            Amount = 5,
            SchemaVersion = 0
        };

        InvokeEnsureMigrated(save);
        CoinSafeAccountEntry guest = GetGuestEntry(save);
        Assert.That(guest.Amount, Is.EqualTo(5));
        Assert.That(save.Amount, Is.EqualTo(-1));

        save.Amount = 99;
        InvokeEnsureMigrated(save);

        Assert.That(GetGuestEntry(save).Amount, Is.EqualTo(5));
        Assert.That(save.Accounts.Count(entry => entry.OwnerKey == guest.OwnerKey), Is.EqualTo(1));
    }

    [Test]
    public void AccountList_RoundTripsPendingAndDirtyStateThroughJsonUtility()
    {
        CoinSafeSave source = new CoinSafeSave
        {
            SchemaVersion = 2,
            ActiveOwnerKey = "facebook:uid-a",
            PendingFacebookUid = "uid-b",
            PendingSourceOwnerKey = "guest:device",
            Accounts = new List<CoinSafeAccountEntry>
            {
                new CoinSafeAccountEntry
                {
                    OwnerKey = "guest:device",
                    Amount = 5,
                    Dirty = true,
                    Revision = 3
                },
                new CoinSafeAccountEntry
                {
                    OwnerKey = "facebook:uid-a",
                    Amount = 0,
                    Dirty = false,
                    Revision = 8
                }
            }
        };

        CoinSafeSave restored = JsonUtility.FromJson<CoinSafeSave>(JsonUtility.ToJson(source));

        Assert.That(restored.Accounts.Count, Is.EqualTo(2));
        Assert.That(restored.ActiveOwnerKey, Is.EqualTo("facebook:uid-a"));
        Assert.That(restored.PendingFacebookUid, Is.EqualTo("uid-b"));
        Assert.That(restored.PendingSourceOwnerKey, Is.EqualTo("guest:device"));
        Assert.That(restored.Accounts[0].Amount, Is.EqualTo(5));
        Assert.That(restored.Accounts[0].Dirty, Is.True);
        Assert.That(restored.Accounts[0].Revision, Is.EqualTo(3));
    }

    [Test]
    public void AddThenReset_AdvancesRevisionAndKeepsLatestZeroDirty()
    {
        CoinSafeAccountEntry entry = new CoinSafeAccountEntry
        {
            OwnerKey = "facebook:uid-a"
        };

        InvokeSetLocalAmount(entry, 7);
        long uploadRevision = entry.Revision;
        InvokeSetLocalAmount(entry, 0);

        Assert.That(entry.Amount, Is.Zero);
        Assert.That(entry.Dirty, Is.True);
        Assert.That(entry.Revision, Is.GreaterThan(uploadRevision));
    }

    [Test]
    public void FacebookAuthTransition_BlocksCloudWritesUntilTheTransitionEnds()
    {
        CoinSafeProgress.BeginFacebookAuthTransition();

        Assert.That(CoinSafeProgress.IsFacebookCloudWriteBlocked, Is.True);

        CoinSafeProgress.EndFacebookAuthTransition();
        Assert.That(CoinSafeProgress.IsFacebookCloudWriteBlocked,
            Is.EqualTo(CoinSafeProgress.HasPendingFacebookResolution));
    }

    [Test]
    public void MissingCloudDocument_NormalizesProgressBoxToZero()
    {
        MethodInfo method = typeof(FirebaseProfileHandler).GetMethod("CreateEmptyProgress", PrivateStatic);
        Assert.That(method, Is.Not.Null);

        FirebasePlayerProgress progress = (FirebasePlayerProgress)method.Invoke(null, new object[] { "uid-a" });

        Assert.That(progress.CloudStateKnown, Is.True);
        Assert.That(progress.CoinSafeAmount, Is.Zero);
        Assert.That(progress.HasCoinSafeAmount, Is.False);
    }

    [Test]
    public void ConflictDialog_UsesLocalCloudTitlesAndBoundProgressBoxValues()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConflictDialogPath);
        Assert.That(prefab, Is.Not.Null);

        SaveProgressFoundDialogView view = prefab.GetComponent<SaveProgressFoundDialogView>();
        Assert.That(view, Is.Not.Null);

        SerializedObject serializedView = new SerializedObject(view);
        Assert.That(serializedView.FindProperty("deviceCoinSafeAmountText").objectReferenceValue, Is.Not.Null);
        Assert.That(serializedView.FindProperty("serverCoinSafeAmountText").objectReferenceValue, Is.Not.Null);

        Transform localCard = FindDeep(prefab.transform, "Device Card");
        Transform cloudCard = FindDeep(prefab.transform, "Server Card");
        Assert.That(localCard.Find("Card Title").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Local Version"));
        Assert.That(cloudCard.Find("Card Title").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Cloud Version"));
        Assert.That(localCard.Find("Collection Label").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Progress Box"));
        Assert.That(cloudCard.Find("Collection Label").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Progress Box"));
    }

    private static void InvokeEnsureMigrated(CoinSafeSave save)
    {
        MethodInfo method = typeof(CoinSafeProgress).GetMethod("EnsureMigrated", PrivateStatic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, new object[] { save });
    }

    private static void InvokeSetLocalAmount(CoinSafeAccountEntry entry, int amount)
    {
        MethodInfo method = typeof(CoinSafeProgress).GetMethod("SetLocalAmount", PrivateStatic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(null, new object[] { entry, amount, false });
    }

    private static CoinSafeAccountEntry GetGuestEntry(CoinSafeSave save)
    {
        CoinSafeAccountEntry guest = save.Accounts.SingleOrDefault(
            entry => entry != null && entry.OwnerKey.StartsWith("guest:"));
        Assert.That(guest, Is.Not.Null);
        return guest;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true).First(transform => transform.name == name);
    }
}
