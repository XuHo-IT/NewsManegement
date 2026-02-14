using Microsoft.AspNetCore.Routing;

namespace FUNewsManagementSystem.Routing
{
    public class ShortRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (values.TryGetValue(routeKey, out var value))
            {
                var valueString = value?.ToString();
                return short.TryParse(valueString, out _);
            }
            return false;
        }
    }
}
