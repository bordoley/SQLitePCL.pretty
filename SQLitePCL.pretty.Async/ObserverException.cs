/*
   Copyright 2014 David Bordoley

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;

namespace SQLitePCL.pretty
{
    internal static class ObserverExtension
    {
        public static void OnNextSafe<T> (this IObserver<T> observer, T result)
        {
            try 
            {
                observer.OnNext(result);
            }
            catch (Exception e)
            {
                throw new ObserverException(e);
            }
        }

        public static void OnCompletedSafe<T> (this IObserver<T> observer)
        {
            try 
            {
                observer.OnCompleted();
            }
            catch (Exception e)
            {
                throw new ObserverException(e);
            }
        }
    }

    internal class ObserverException : Exception
    {
        internal ObserverException(Exception e) : base("", e) { }
    }
}
