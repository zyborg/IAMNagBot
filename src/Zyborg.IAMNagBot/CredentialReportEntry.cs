using System;
using CsvHelper.Configuration.Attributes;

namespace Zyborg.IAMNagBot
{
    public class CredentialReportEntry
    {
        [Name("user")]
        public string User { get; set; }

        [Name("arn")]
        public string Arn { get; set; }

        [Name("user_creation_time")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        public DateTime? UserCreationTime { get; set; }

        [Name("password_enabled")]
        [Default(false)]
        [NullValues("N/A", "not_supported", "no_information")]
        [BooleanTrueValues("TRUE")]
        [BooleanFalseValues("FALSE")]
        public bool? PasswordEnabled { get; set; }

        [Name("password_last_used")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        public DateTime? PasswordLastUsed { get; set; }

        [Name("password_last_changed")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        public DateTime? PasswordLastChanged { get; set; }

        [Name("password_next_rotation")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        public DateTime? PasswordNextRotation { get; set; }

        [Name("mfa_active")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        [BooleanTrueValues("TRUE")]
        [BooleanFalseValues("FALSE")]
        public bool? MfaActive { get; set; }

        [Name("access_key_1_active")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        [BooleanTrueValues("TRUE")]
        [BooleanFalseValues("FALSE")]
        public bool? AccessKey1Active { get; set; }

        [Name("access_key_1_last_rotated")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        public DateTime? AccessKey1LastRotated { get; set; }

        [Name("access_key_1_last_used_date")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        public DateTime? AccessKey1LastUsedDate { get; set; }

        [Name("access_key_1_last_used_region")]
        [Default(null)]
        [NullValues("N/A")]
        public string AccessKey1LastUsedRegion { get; set; }

        [Name("access_key_1_last_used_service")]
        [Default(null)]
        [NullValues("N/A")]
        public string AccessKey1LastUsedService { get; set; }

        [Name("access_key_2_active")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        [BooleanTrueValues("TRUE")]
        [BooleanFalseValues("FALSE")]
        public bool? AccessKey2Active { get; set; }

        [Name("access_key_2_last_rotated")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        public DateTime? AccessKey2LastRotated { get; set; }

        [Name("access_key_2_last_used_date")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        public DateTime? AccessKey2LastUsedDate { get; set; }

        [Name("access_key_2_last_used_region")]
        [Default(null)]
        [NullValues("N/A")]
        public string AccessKey2LastUsedRegion { get; set; }

        [Name("access_key_2_last_used_service")]
        [Default(null)]
        [NullValues("N/A")]
        public string AccessKey2LastUsedService { get; set; }

        [Name("cert_1_active")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        [BooleanTrueValues("TRUE")]
        [BooleanFalseValues("FALSE")]
        public bool? Cert1Active { get; set; }

        [Name("cert_1_last_rotated")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        public DateTime? Cert1LastRotated { get; set; }

        [Name("cert_2_active")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        [BooleanTrueValues("TRUE")]
        [BooleanFalseValues("FALSE")]
        public bool? Cert2Active{ get; set; }

        [Name("cert_2_last_rotated")]
        [Default(null)]
        [NullValues("N/A", "not_supported", "no_information")]
        public DateTime? Cert2LastRotated { get; set; }
    }
}