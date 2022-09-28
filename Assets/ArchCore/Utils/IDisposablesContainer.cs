using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArchCore.Utils
{
    public interface IDisposablesContainer {
        void RegisterForDispose(IDisposable disposable);
    }
}