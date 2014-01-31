//  Original author - Josh Smith - http://msdn.microsoft.com/en-us/magazine/dd419663.aspx#id0090030
//Event Design: http://msdn.microsoft.com/en-us/library/ms229011.aspx
namespace NetInject.Utils.MicroMvvm {
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using Annotations;
    [Serializable]
    public abstract class ObservableObject : INotifyPropertyChanged {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(PropertyChangedEventArgs e) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, e);
        }
        [UsedImplicitly]
        protected void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpresssion) {
            string propertyName = PropertySupport.ExtractPropertyName(propertyExpresssion);
            RaisePropertyChanged(propertyName);
        }
        protected void RaisePropertyChanged(String propertyName) {
            VerifyPropertyName(propertyName);
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
        /// <summary>
        ///     Warns the developer if this Object does not have a public property with
        ///     the specified name. This method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        private void VerifyPropertyName(String propertyName) {
            // verify that the property name matches a real,
            // public, instance property on this Object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
                Debug.Fail("Invalid property name: " + propertyName);
        }
    }
}