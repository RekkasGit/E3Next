using System;
using System.Collections.Generic;
using System.Linq;

namespace IniParser.Model
{
    /// <summary>
    ///     Information associated to a key from an INI file.
    ///     Includes both the value and the comments associated to the key.
    /// </summary>
    public class KeyData : ICloneable
    {
        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="KeyData"/> class.
        /// </summary>
        public KeyData(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                throw new ArgumentException("key name can not be empty");

            _comments = new List<string>();
            ValueList = new List<string>();
            _keyName = keyName;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KeyData"/> class
        ///     from a previous instance of <see cref="KeyData"/>.
        /// </summary>
        /// <remarks>
        ///     Data is deeply copied
        /// </remarks>
        /// <param name="ori">
        ///     The instance of the <see cref="KeyData"/> class 
        ///     used to create the new instance.
        /// </param>
        public KeyData(KeyData ori)
        {
            ValueList = new List<string>(ori.ValueList);
            _keyName = ori._keyName;
            _comments = new List<string>(ori._comments);
        }

        #endregion Constructors 

        #region Properties 

        /// <summary>
        /// Gets or sets the comment list associated to this key.
        /// </summary>
        public List<string> Comments
        {
            get { return _comments; }
            set
            {
                _comments.AddRange(value);
                //_comments = new List<string> (value) ; 
            }
        }

        /// <summary>
        ///     Gets or sets the value associated to this key.
        /// </summary>
        public string Value
        {
            get { return ValueList.LastOrDefault() ?? string.Empty; }
            set
            {

                _valueList.Add(value);
            }
        }


        public List<string> ValueList
        {
            get { return _valueList; }
            private set { _valueList = value; }
        }

        /// <summary>
        ///     Gets or sets the name of the key.
        /// </summary>
        public string KeyName
        {
            get
            {
                return _keyName;
            }

            set
            {
                if (value != string.Empty)
                    _keyName = value;
            }

        }

        #endregion Properties 

        #region ICloneable Members

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return new KeyData(this);
        }

        #endregion

        #region Non-public Members

        // List with comment lines associated to this key 
        private List<string> _comments;

        // Name of the current key
        private string _keyName;
        private List<string> _valueList = new List<string>();

        #endregion
    }
}
