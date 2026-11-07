#pragma warning disable 0649

using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    public class ProjectInitSettings : ScriptableObject
    {
        [SerializeField] InitModule[] modules;
        public InitModule[] Modules => modules;
        [NonSerialized] private HashSet<InitModule> prepared;
        [NonSerialized] private HashSet<InitModule> initialized;
        public string LastError { get; private set; }
        public string FailedModule { get; private set; }

        public List<string> ValidateModules()
        {
            var errors = new List<string>();
            var types = new HashSet<Type>();
            if (modules == null) { errors.Add("Module list is missing."); return errors; }
            foreach (var module in modules)
            {
                if (module == null) { errors.Add("Module list contains a missing module."); continue; }
                if (!types.Add(module.GetType())) errors.Add("Duplicate module: " + module.ModuleName);
            }
            var preceding = new HashSet<Type>();
            foreach (var module in modules)
            {
                if (module == null) continue;
                foreach (var dependency in module.Dependencies)
                    if (!preceding.Contains(dependency)) errors.Add(module.ModuleName + " requires an earlier " + dependency.Name + ".");
                preceding.Add(module.GetType());
                try { module.ValidateConfiguration(); }
                catch (Exception exception) { errors.Add(module.ModuleName + ": " + exception.Message); }
            }
            return errors;
        }

        internal IEnumerator Prepare()
        {
            prepared ??= new HashSet<InitModule>();
            initialized ??= new HashSet<InitModule>();
            LastError = null;
            FailedModule = null;
            var errors = ValidateModules();
            if (errors.Count > 0) { LastError = string.Join("\n", errors); yield break; }
            foreach (InitModule module in modules)
            {
                if (prepared.Contains(module)) continue;
                // Flatten nested enumerators so errors reach the bootstrap instead of stranding its coroutine.
                var stack = new Stack<IEnumerator>();
                try { var routine = module.Prepare(); if (routine != null) stack.Push(routine); }
                catch (Exception exception) { SetFailure(module, exception); }
                while (stack.Count > 0 && LastError == null)
                {
                    object current = null;
                    try
                    {
                        var routine = stack.Peek();
                        if (!routine.MoveNext()) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                        current = routine.Current;
                    }
                    catch (Exception exception) { SetFailure(module, exception); }
                    if (LastError != null) break;
                    if (current is IEnumerator nested) stack.Push(nested);
                    else yield return current;
                }
                if (LastError != null)
                {
                    while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
                    yield break;
                }
                prepared.Add(module);
            }
        }

        public void Init(Initializer initializer)
        {
            initialized ??= new HashSet<InitModule>();
            for (int i = 0; i < modules.Length; i++)
            {
                if(modules[i] != null && !initialized.Contains(modules[i]))
                {
                    try { modules[i].CreateComponent(); initialized.Add(modules[i]); }
                    catch (Exception exception)
                    {
                        SetFailure(modules[i], exception);
                        modules[i].Shutdown();
                        throw;
                    }
                }
            }
        }

        private void SetFailure(InitModule module, Exception exception)
        {
            FailedModule = module.ModuleName;
            LastError = module.ModuleName + ": " + exception.Message;
        }

        public void Shutdown()
        {
            if (initialized != null && modules != null)
                for (int i = modules.Length - 1; i >= 0; i--)
                    if (modules[i] != null && initialized.Contains(modules[i]))
                        try { modules[i].Shutdown(); }
                        catch (Exception exception) { Debug.LogException(exception); }
            initialized?.Clear();
            prepared?.Clear();
        }

        public T GetModule<T>() where T : InitModule
        {
            foreach (var module in modules)
            {
                if (module != null && module is T)
                {
                    return (T)module;
                }
            }

            return null;
        }
    }
}
