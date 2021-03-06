// Copyright (c) Parbad. All rights reserved.
// Licensed under the GNU GENERAL PUBLIC License, Version 3.0. See License.txt in the project root for license information.

using System;

namespace Parbad.Abstraction
{
    public interface IGatewayProvider
    {
        IGateway Provide(Type gatewayType);
    }
}
