import React, { useState, useCallback, useEffect } from 'react';
import { useDropzone } from 'react-dropzone';
import axios from 'axios';

// Material-UI importları...
import {
    Box,
    Card,
    CardContent,
    Typography,
    CircularProgress,
    Chip,
    Alert,
    Paper,
    Stack,
    Accordion,
    AccordionSummary,
    AccordionDetails,
    Skeleton,
    TextField,
    MenuItem,
    Select,
    FormControl,
    InputLabel
} from '@mui/material';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutline';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import LightbulbIcon from '@mui/icons-material/Lightbulb';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import FeedbackIcon from '@mui/icons-material/Feedback';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import KeyIcon from '@mui/icons-material/VpnKey';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
// import FlagTR from '../assets/flag-tr.svg';
// import FlagGB from '../assets/flag-gb.svg';
import { useTheme, ThemeProvider, createTheme } from '@mui/material/styles';
import Switch from '@mui/material/Switch';

interface AnalysisReport {
    atsScore: number;
    contentScore: number;
    foundKeywords: string[];
    missingKeywords: string[];
    positiveFeedback: string[];
    suggestions: string[];
    extractedCvText: string;
    atsImprovementTips: string[];
    extraAdvice: string[];
}

const CircularProgressWithLabel = (props: { value: number }) => (
    <Box sx={{ position: 'relative', display: 'inline-flex' }}>
        <CircularProgress variant="determinate" {...props} size={120} thickness={4} />
        <Box
            sx={{
                top: 0,
                left: 0,
                bottom: 0,
                right: 0,
                position: 'absolute',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
            }}
        >
            <Typography variant="h4" component="div" color="primary.main" sx={{ fontWeight: 'bold' }}>
                {`${Math.round(props.value)}`}
                <Typography variant="h6" component="span" color="text.secondary">/100</Typography>
            </Typography>
        </Box>
    </Box>
);

export const CvAnalyzerPage: React.FC = () => {
    const [jobDescription, setJobDescription] = useState<string>('');
    const [analysisReport, setAnalysisReport] = useState<AnalysisReport | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [error, setError] = useState<string | null>(null);
    const [lang, setLang] = useState<'tr' | 'en'>('tr');
    const [showLangBox, setShowLangBox] = useState(true);
    const theme = createTheme({
      palette: {
        mode: 'light', // Always light mode
        primary: { main: '#6366f1' },
      },
    });

    const t = (tr: string, en: string) => lang === 'tr' ? tr : en;

    useEffect(() => {
      const onScroll = () => {
        if (window.scrollY === 0) {
          setShowLangBox(true);
        } else {
          setShowLangBox(false);
        }
      };
      window.addEventListener('scroll', onScroll);
      return () => window.removeEventListener('scroll', onScroll);
    }, []);

    const onDrop = useCallback(async (acceptedFiles: File[]) => {
        const file = acceptedFiles[0];
        if (!file) return;

        setIsLoading(true);
        setError(null);
        setAnalysisReport(null);

        const formData = new FormData();
        formData.append('file', file);
        formData.append('jobDescription', jobDescription || '');
        formData.append('lang', lang);

        try {
            // Port numarasını doğru ayarlayın - HTTP kullanın, HTTPS değil
            const API_BASE_URL = 'http://localhost:5191'; // HTTP kullanın
            
            const response = await axios.post<AnalysisReport>(
                `${API_BASE_URL}/api/CvAnalysis/upload`, 
                formData,
                {
                    headers: {
                        'Content-Type': 'multipart/form-data',
                    },
                    timeout: 30000, // 30 saniye timeout
                }
            );
            
            setAnalysisReport(response.data);
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        } catch (err: any) {
            console.error('Detailed error:', err);
            
            if (err.response) {
                // Server responded with error
                console.error('Error response:', err.response.data);
                setError(`Server hatası: ${err.response.data.error || err.response.data.message || 'Bilinmeyen hata'}`);
            } else if (err.request) {
                // Request was made but no response received
                console.error('Network error:', err.request);
                setError('Sunucuya bağlanılamadı. Lütfen backend servisinin çalıştığından emin olun.');
            } else {
                // Something else happened
                console.error('Error message:', err.message);
                setError(`Beklenmeyen hata: ${err.message}`);
            }
        } finally {
            setIsLoading(false);
        }
    }, [jobDescription, lang]);

    const { getRootProps, getInputProps, isDragActive } = useDropzone({
        onDrop,
        accept: {
            'application/pdf': ['.pdf'],
            'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx']
        },
        maxFiles: 1,
        maxSize: 10 * 1024 * 1024, // 10MB limit
    });

    // Test backend connection
    const testConnection = async () => {
        try {
            const response = await axios.get('http://localhost:5191/api/CvAnalysis/test');
            console.log('Test response:', response.data);
        } catch (err) {
            console.error('Test failed:', err);
        }
    };

    React.useEffect(() => {
        testConnection();
    }, []);

    const LANG_FLAGS = { tr: '🇹🇷', en: '🇬🇧' };

    return (
        <ThemeProvider theme={theme}>
            <>
                {/* Sağ üstte sabit dil seçici kutusu, ThemeProvider'ın da dışında */}
                {/* Select ve FormControl'u kaldır, sadece bayrak img ve Box kullan */}
                <Box
                    sx={{
                        position: 'fixed',
                        top: { xs: 10, md: 18 },
                        right: { xs: 10, md: 24 },
                        zIndex: 3000,
                        bgcolor: 'rgba(255,255,255,0.7)',
                        backdropFilter: 'blur(8px)',
                        borderRadius: 6,
                        boxShadow: 8,
                        border: '2px solid #6366f1',
                        width: 60,
                        height: 30,
                        px: 0,
                        py: 0,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        minWidth: 0,
                        gap: 0,
                        pointerEvents: showLangBox ? 'auto' : 'none',
                        opacity: showLangBox ? 1 : 0,
                        transition: 'opacity 0.3s',
                        overflow: 'hidden',
                        cursor: 'pointer',
                    }}
                    onClick={() => setLang(lang === 'tr' ? 'en' : 'tr')}
                >
                    <img
                        src={lang === 'tr'
                            ? 'https://upload.wikimedia.org/wikipedia/commons/b/b4/Flag_of_Turkey.svg'
                            : 'https://bayrakevi.com/image/cache/data/devlet1/ingiliz-800x800.jpg'}
                        alt={lang === 'tr' ? 'TR' : 'GB'}
                        style={{
                            width: '100%',
                            height: '100%',
                            objectFit: 'cover',
                            display: 'block',
                            margin: 0,
                            borderRadius: 0,
                            background: 'transparent',
                        }}
                    />
                </Box>
                {/* Ana içerik kutusu */}
                <Box sx={{
                    p: { xs: 1, md: 4 },
                    maxWidth: '1400px',
                    margin: 'auto',
                    minHeight: '100vh',
                    background: 'linear-gradient(135deg, #e0e7ff 0%, #f4f6f8 100%)',
                    position: 'relative',
                    transition: 'background 0.5s',
                }}>
                    <Paper elevation={0} sx={{
                        p: 4, mb: 4, textAlign: 'center', borderRadius: 3,
                        background: 'linear-gradient(90deg, #6366f1 0%, #60a5fa 100%)',
                        color: 'white',
                        boxShadow: 3
                    }}>
                        <Typography variant="h3" component="h1" sx={{ fontWeight: 'bold', letterSpacing: 1 }}>
                            {t('Yapay Zeka Destekli CV Analiz Motoru', 'AI-Powered CV Analysis Engine')}
                        </Typography>
                        <Typography variant="subtitle1" sx={{ color: 'rgba(255,255,255,0.85)' }}>
                            {t('CV\'nizi yükleyerek ATS uyumluluğunu ve içerik kalitesini saniyeler içinde ölçün.', 'Upload your CV to instantly measure ATS compatibility and content quality.')}
                        </Typography>
                    </Paper>
                    <Box sx={{
                        display: 'grid',
                        gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
                        gap: 4,
                        mb: 4
                    }}>
                        {/* İş İlanı Metin Kutusu */}
                        <Card sx={{ height: '100%' }} elevation={2}>
                            <CardContent>
                                <Typography variant="h6" gutterBottom>{t('1. Adım: İş İlanı Metnini Yapıştırın (İsteğe Bağlı)', 'Step 1: Paste Job Description (Optional)')}
                                </Typography>
                                <TextField
                                    fullWidth
                                    multiline
                                    rows={8}
                                    variant="outlined"
                                    label={t('İş İlanı Metni', 'Job Description')}
                                    placeholder={t('Karşılaştırma yapmak istediğiniz iş ilanı metnini buraya yapıştırın...', 'Paste the job description you want to compare here...')}
                                    value={jobDescription}
                                    onChange={(e) => setJobDescription(e.target.value)}
                                />
                            </CardContent>
                        </Card>

                        {/* CV Yükleme Alanı */}
                        <Card sx={{ height: '100%' }} elevation={2}>
                            <CardContent>
                                <Typography variant="h6" gutterBottom>{t('2. Adım: CV\'nizi Yükleyin ve Analiz Edin', 'Step 2: Upload Your CV and Analyze')}</Typography>
                                <Box {...getRootProps()} sx={{ 
                                    border: `3px dashed ${isDragActive ? '#3f51b5' : 'lightgrey'}`, 
                                    p: 4, 
                                    textAlign: 'center', 
                                    cursor: 'pointer', 
                                    mt: 2, 
                                    borderRadius: 2, 
                                    bgcolor: isDragActive ? '#e8eaf6' : 'white', 
                                    transition: 'background-color 0.3s ease', 
                                    height: '200px',
                                    display: 'flex',
                                    flexDirection: 'column',
                                    justifyContent: 'center'
                                }}>
                                    <input {...getInputProps()} />
                                    <UploadFileIcon sx={{ fontSize: 40, color: 'grey.500', mb: 1 }} />
                                    <Typography>{isDragActive ? t('Dosyayı Buraya Bırakın...', 'Drop your file here...') : t('Analiz İçin CV Dosyanızı Sürükleyin', 'Drag and drop your CV file here, or click to select')}</Typography>
                                    <Typography color="text.secondary" variant="body2">{t('veya seçmek için tıklayın (.pdf, .docx)', 'or click to select (.pdf, .docx)')}
                                    </Typography>
                                </Box>
                            </CardContent>
                        </Card>
                    </Box>
                    {isLoading && (
                        <Stack spacing={3}>
                            <Skeleton variant="rectangular" animation="wave" height={150} />
                            <Box sx={{ display: 'flex', gap: 3 }}>
                                <Skeleton variant="rectangular" animation="wave" sx={{ flex: 5 }} height={200} />
                                <Skeleton variant="rectangular" animation="wave" sx={{ flex: 7 }} height={200} />
                            </Box>
                        </Stack>
                    )}
                    
                    {error && <Alert severity="error" variant="filled" sx={{ mb: 3 }}>{error}</Alert>}

                    {analysisReport && (
                        <Box sx={{
                            display: 'flex',
                            flexDirection: { xs: 'column', md: 'row' },
                            gap: 4,
                            alignItems: 'flex-start',
                            mt: 2
                        }}>
                            <Box sx={{ flex: { md: 5 }, minWidth: 0 }}>
                                <Stack spacing={3}>
                                    <Card elevation={4} sx={{ borderRadius: 3, boxShadow: 6, transition: 'transform 0.2s', '&:hover': { transform: 'scale(1.03)' } }}>
                                        <CardContent sx={{ textAlign: 'center', p: 4 }}>
                                            <TrendingUpIcon sx={{ fontSize: 36, color: '#6366f1', mb: 1 }} />
                                            <Typography variant="h5" component="div" gutterBottom sx={{ fontWeight: '600' }}>
                                                {t('ATS Uyumluluk Puanı', 'ATS Compatibility Score')}
                                            </Typography>
                                            <CircularProgressWithLabel value={analysisReport.atsScore} />
                                        </CardContent>
                                    </Card>
                                    <Card elevation={2} sx={{ borderRadius: 3, boxShadow: 3, transition: 'transform 0.2s', '&:hover': { transform: 'scale(1.02)' } }}>
                                        <CardContent>
                                            <FeedbackIcon sx={{ fontSize: 28, color: '#22c55e', mb: 1 }} />
                                            <Typography variant="h6" gutterBottom>{t('Öneriler ve Geri Bildirimler', 'Suggestions and Feedback')}</Typography>
                                            {analysisReport.positiveFeedback.map((fb, i) => (
                                                <Alert key={i} icon={<CheckCircleIcon fontSize="inherit" />} severity="success" sx={{ mb: 1 }}>
                                                    {fb}
                                                </Alert>
                                            ))}
                                            {analysisReport.suggestions.map((sug, i) => (
                                                <Alert key={i} severity="info" sx={{ mb: 1 }}>
                                                    {sug}
                                                </Alert>
                                            ))}
                                        </CardContent>
                                    </Card>
                                    <Card elevation={2} sx={{ borderRadius: 3, boxShadow: 3, transition: 'transform 0.2s', '&:hover': { transform: 'scale(1.02)' } }}>
                                        <CardContent>
                                            <TrendingUpIcon sx={{ fontSize: 28, color: '#f59e42', mb: 1 }} />
                                            <Typography variant="h6" gutterBottom>{t('ATS Puanını Yükseltmek İçin Tavsiyeler', 'Tips to Improve ATS Score')}</Typography>
                                            <ul style={{ marginTop: 8, marginBottom: 0 }}>
                                                {analysisReport.atsImprovementTips && analysisReport.atsImprovementTips.map((tip, i) => (
                                                    <li key={i} style={{ marginBottom: 6 }}>
                                                        <Typography variant="body2">{tip}</Typography>
                                                    </li>
                                                ))}
                                            </ul>
                                        </CardContent>
                                    </Card>
                                </Stack>
                            </Box>
                            <Box sx={{ flex: { md: 7 }, minWidth: 0 }}>
                                <Stack spacing={3}>
                                    <Card elevation={2} sx={{ borderRadius: 3, boxShadow: 3, transition: 'transform 0.2s', '&:hover': { transform: 'scale(1.02)' } }}>
                                        <CardContent>
                                            <KeyIcon sx={{ fontSize: 28, color: '#6366f1', mb: 1 }} />
                                            <Typography variant="h6">{t('Bulunan Anahtar Kelimeler', 'Found Keywords')}</Typography>
                                            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mt: 2 }}>
                                                {analysisReport.foundKeywords.length > 0 ? 
                                                    analysisReport.foundKeywords.map(keyword => (
                                                        <Chip key={keyword} label={keyword} color="success" variant="filled" />
                                                    )) : 
                                                    <Typography variant="body2" color="text.secondary">
                                                        {t('Hedeflenen anahtar kelimelerden hiçbiri bulunamadı.', 'None of the targeted keywords were found.')}
                                                    </Typography>
                                                }
                                            </Box>
                                        </CardContent>
                                    </Card>
                                    <Card elevation={2} sx={{ borderRadius: 3, boxShadow: 3, transition: 'transform 0.2s', '&:hover': { transform: 'scale(1.02)' } }}>
                                        <CardContent>
                                            <WarningAmberIcon sx={{ fontSize: 28, color: '#f59e42', mb: 1 }} />
                                            <Typography variant="h6">{t('Eksik Anahtar Kelimeler', 'Missing Keywords')}</Typography>
                                            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mt: 2 }}>
                                                {analysisReport.missingKeywords.length > 0 ? 
                                                    analysisReport.missingKeywords.map(keyword => (
                                                        <Chip key={keyword} label={keyword} color="warning" variant="outlined" />
                                                    )) : 
                                                    <Typography variant="body2" color="text.secondary">
                                                        {t('Tebrikler! Hedeflenen tüm anahtar kelimeler CV\'nizde mevcut.', 'Congratulations! All targeted keywords are present in your CV.')}
                                                    </Typography>
                                                }
                                            </Box>
                                        </CardContent>
                                    </Card>
                                    {/* Ekstra faydalı öneriler kartı burada! */}
                                    <Card elevation={3} sx={{ borderRadius: 3, boxShadow: 6, background: 'linear-gradient(90deg, #fef9c3 0%, #f0e68c 100%)', transition: 'transform 0.2s', '&:hover': { transform: 'scale(1.03)' } }}>
                                        <CardContent>
                                            <LightbulbIcon sx={{ fontSize: 32, color: '#f59e42', mb: 1 }} />
                                            <Typography variant="h6" gutterBottom>{t('Ekstra Faydalı Öneriler', 'Extra Useful Tips')}</Typography>
                                            <ul style={{ marginTop: 8, marginBottom: 0 }}>
                                                {analysisReport.extraAdvice && analysisReport.extraAdvice.map((advice, i) => (
                                                    <li key={i} style={{ marginBottom: 6 }}>
                                                        <Typography variant="body2">{advice}</Typography>
                                                    </li>
                                                ))}
                                            </ul>
                                        </CardContent>
                                    </Card>
                                </Stack>
                            </Box>
                        </Box>
                    )}
                </Box>
            </>
        </ThemeProvider>
    );
};