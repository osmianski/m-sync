using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Manadev.Sync
{
    public static class ListExtensions
    {
        public static bool Affects(this List<FileChange> changes, string path)
        {
            foreach (var change in changes)
            {
                // if parent directory changed, update the path
                if (path.StartsWith(change.Path))
                {
                    return true;
                }

                // in case anything changed within a module, update the path
                if (change.Path.StartsWith(path))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
