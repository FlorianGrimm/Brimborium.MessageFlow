namespace Brimborium.MessageFlow.Internal;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

internal static class ObjectPoolStringBuilder {
    private static DefaultObjectPool<StringBuilder> _ObjectPoolStringBuilder = new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy());
    public static StringBuilder Get() => _ObjectPoolStringBuilder.Get();

    public static void Return(StringBuilder value) => _ObjectPoolStringBuilder.Return(value);
}
