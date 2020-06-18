using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Hiarc.Core.Models;
using Xunit.Sdk;

namespace HiarcIntegrationTest
{
    public class EntityComparer : IEqualityComparer<Entity>
    {
        public bool Equals([AllowNull] Entity x, [AllowNull] Entity y)
        {
            if (x.Type != y.Type)
            {
                return false;
                //throw new EqualException($"Expected Type: '{x.Type}'", $"Actual Type: '{y.Type}'");
            }
            else if (x.Key != y.Key)
            {
                return false;
                //throw new EqualException($"Expected Key: '{x.Key}'", $"Actual Key: '{y.Key}'");
            }
            else if (x.Name != y.Name)
            {
                return false;
                //throw new EqualException($"Expected Name: '{x.Name}'", $"Actual Name: '{y.Name}'");
            }
            else if (x.Description != y.Description)
            {
                return false;
                //throw new EqualException($"Expected Description: '{x.Description}'", $"Actual Description: '{y.Description}'");
            }
            else if (x.CreatedBy != y.CreatedBy)
            {
                return false;
                //throw new EqualException($"Expected CreatedBy: '{x.CreatedBy}'", $"Actual CreatedBy: '{y.CreatedBy}'");
            }
            else if (x.CreatedAt != y.CreatedAt)
            {
                return false;
                //throw new EqualException($"Expected CreatedAt: '{x.CreatedAt}'", $"Actual CreatedAt: '{y.CreatedAt}'");
            }
            else if (x.ModifiedAt != y.ModifiedAt)
            {
                return false;
                //throw new EqualException($"Expected ModifiedAt: '{x.ModifiedAt}'", $"Actual ModifiedAt: '{y.ModifiedAt}'");
            } 
            else
            {
                return true;
            }            
        }

        public int GetHashCode([DisallowNull] Entity obj)
        {
            throw new System.NotImplementedException();
        }
    }
}