/* USER CODE BEGIN Header */
/**
  ******************************************************************************
  * @file           : main.c
  * @brief          : Main program body
  ******************************************************************************
  * @attention
  *
  * Copyright (c) 2025 STMicroelectronics.
  * All rights reserved.
  *
  * This software is licensed under terms that can be found in the LICENSE file
  * in the root directory of this software component.
  * If no LICENSE file comes with this software, it is provided AS-IS.
  *
  ******************************************************************************
  */
/* USER CODE END Header */
/* Includes ------------------------------------------------------------------*/
#include "main.h"
#include "i2c.h"
#include "tim.h"
#include "usart.h"
#include "gpio.h"

/* Private includes ----------------------------------------------------------*/
/* USER CODE BEGIN Includes */
#include <stdio.h>  // Necesario para printf y sscanf
/* USER CODE END Includes */

/* Private typedef -----------------------------------------------------------*/
/* USER CODE BEGIN PTD */

/* USER CODE END PTD */

/* Private define ------------------------------------------------------------*/
/* USER CODE BEGIN PD */

/* USER CODE END PD */

/* Private macro -------------------------------------------------------------*/
/* USER CODE BEGIN PM */

/* USER CODE END PM */

/* Private variables ---------------------------------------------------------*/

/* USER CODE BEGIN PV */
uint8_t btn_lim, btn_su, btn1, btn2, btn3, btn4;
uint8_t bl_last=0, bs_last=0, b1_last=0, b2_last=0, b3_last=0, b4_last=0;
uint16_t angulo_anterior_1 = 0, angulo_anterior_2 = 0;
int32_t enc1_accumulator = 0, enc2_accumulator = 0;
const int32_t MAGNETIC_THRESHOLD_1 = 200; // Para el volante 1 e2
const int32_t MAGNETIC_THRESHOLD_2 = 120; // Para el volante 2 e1
int8_t enc1_send = 0, enc2_send = 0, activate = 0;

volatile int32_t encoder_insercion = 0;
uint32_t last_irq_ins = 0;

uint8_t err_encoder = 0;


uint8_t rx_byte;
char rx_buffer[20];
uint8_t rx_index = 0;
uint8_t mensaje_completo = 0;

// VARIABLES PARA EL VIBRADOR
uint32_t tiempo_inicio_vibracion = 0;
uint8_t vibrando = 0;


// Tabla de estados válidos del encoder
// Solo suma 1 o resta -1 si la secuencia física es matemáticamente correcta.
const int8_t tabla_encoder[16] = {0, 1, -1, 0, -1, 0, 0, 1, 1, 0, 0, -1, 0, -1, 1, 0};

// Memoria para guardar el estado del milisegundo anterior
uint8_t estado_ant_1 = 0;
uint8_t estado_ant_2 = 0;

/* USER CODE END PV */

/* Private function prototypes -----------------------------------------------*/
void SystemClock_Config(void);
/* USER CODE BEGIN PFP */

/* USER CODE END PFP */

/* Private user code ---------------------------------------------------------*/
/* USER CODE BEGIN 0 */
#define AS5600_ADDR 0x6C // 0x36 desplazado a la izquierda para STM32
#define RAW_ANGLE_REG 0x0C

// Llamamos a los puertos I2C que configurarás en el CubeIDE
extern I2C_HandleTypeDef hi2c1;
extern I2C_HandleTypeDef hi2c3;


uint8_t readButton(GPIO_TypeDef* GPIOx, uint16_t GPIO_Pin, uint8_t *lastState) {
    uint8_t current = HAL_GPIO_ReadPin(GPIOx, GPIO_Pin);
    uint8_t pressed = 0;

    if (current != *lastState) {
        HAL_Delay(10); // debounce
        current = HAL_GPIO_ReadPin(GPIOx, GPIO_Pin);

        if (current != *lastState) {
            *lastState = current;
            if (current == 1) pressed = 1; // Solo reacciona al soltar/presionar (flanco de subida)
        }
    }
    return pressed;
}

// Leer ángulo del AS5600
uint16_t Leer_Angulo_AS5600(I2C_HandleTypeDef *hi2c, uint16_t angulo_seguro) {
    uint8_t buffer[2];
    // Solicitamos 2 bytes desde el registro 0x0C
    if (HAL_I2C_Mem_Read(hi2c, AS5600_ADDR, RAW_ANGLE_REG, I2C_MEMADD_SIZE_8BIT, buffer, 2, 10) == HAL_OK) {
        uint16_t angulo = (buffer[0] << 8) | buffer[1];
        return angulo & 0x0FFF; // Filtramos la basura para dejar solo 12 bits
    }
    return angulo_seguro; // Si el I2C falla Retorna 0 si el sensor está desconectado
}

int _write(int file, char *ptr, int len) {
    HAL_UART_Transmit(&huart2, (uint8_t*)ptr, len, HAL_MAX_DELAY);
    return len;
}
// INTERRUPCIONES DE LOS ENCODERS (Máquina de Estados)
void HAL_GPIO_EXTI_Callback(uint16_t GPIO_Pin) {

    // ENCODER 1: Inserción (PB5 = CLK, PA12 = DT)
    // Si la interrupción viene de CLK o de DT del Encoder 1...
    if(GPIO_Pin == GPIO_PIN_5 || GPIO_Pin == GPIO_PIN_12) {

        // Leemos el estado físico de ambos pines al mismo tiempo
        uint8_t clk = HAL_GPIO_ReadPin(GPIOB, GPIO_PIN_5);
        uint8_t dt = HAL_GPIO_ReadPin(GPIOA, GPIO_PIN_12);

        // Unimos los bits para formar el estado actual (0, 1, 2 o 3)
        uint8_t estado_actual = (clk << 1) | dt;

        // Creamos un índice combinando el estado anterior con el actual
        uint8_t indice = (estado_ant_1 << 2) | estado_actual;

        // Consultamos la tabla maestra
        int8_t movimiento = tabla_encoder[indice];

        if(movimiento != 0) {
            encoder_insercion += movimiento;
            activate = 1;
        }

        // Guardamos el estado para la próxima vez
        estado_ant_1 = estado_actual;
    }

}
// INTERRUPCIÓN DE RECEPCIÓN UART (Mensajes de Unity)
// Unity debe enviar mensajes terminados en '\n', ej: "V1:100\n"

void HAL_UART_RxCpltCallback(UART_HandleTypeDef *huart) {
    if(huart->Instance == USART2) {

        // 1. CASO  pregunta de identificación?
        if(rx_byte == '?') {
            // Respondemos con la firma única del proyecto
            // El \n es vital para que el puerto serial de Unity sepa que terminó la línea
            printf("ID:ENDOSCOPIO_V1\n");

            // NO guardamos el '?' en el buffer, simplemente reiniciamos la escucha
        }
        // 2. CASO NORMAL: Es un mensaje de control (vibración, etc.)
        else {
            if(rx_byte == '\n' || rx_index >= 19) {
                rx_buffer[rx_index] = '\0'; // Terminamos el string
                mensaje_completo = 1;
                rx_index = 0;
            } else {
                rx_buffer[rx_index++] = rx_byte;
            }
        }

        // 3. CONTINUIDAD: Volvemos a habilitar la escucha del siguiente byte
        HAL_UART_Receive_IT(&huart2, &rx_byte, 1);
    }
}
/* USER CODE END 0 */

/**
  * @brief  The application entry point.
  * @retval int
  */
int main(void)
{

  /* USER CODE BEGIN 1 */

  /* USER CODE END 1 */

  /* MCU Configuration--------------------------------------------------------*/

  /* Reset of all peripherals, Initializes the Flash interface and the Systick. */
  HAL_Init();

  /* USER CODE BEGIN Init */

  /* USER CODE END Init */

  /* Configure the system clock */
  SystemClock_Config();

  /* USER CODE BEGIN SysInit */

  /* USER CODE END SysInit */

  /* Initialize all configured peripherals */
  MX_GPIO_Init();
  MX_I2C1_Init();
  MX_I2C3_Init();
  MX_TIM2_Init();
  MX_USART2_UART_Init();
  /* USER CODE BEGIN 2 */
  printf("\r\n--- INICIANDO TEST CRUDO I2C ---\r\n");
    uint8_t test_buffer[2];

    // Test I2C1 (Volante 1 en PA9/PA10)
    if(HAL_I2C_Mem_Read(&hi2c1, 0x6C, 0x0C, I2C_MEMADD_SIZE_8BIT, test_buffer, 2, 100) == HAL_OK) {
        printf(">>> I2C1 (PA9/PB10) VIVO <<< Angulo: %d\r\n", (test_buffer[0] << 8) | test_buffer[1]);
    } else {
        uint32_t err1 = HAL_I2C_GetError(&hi2c1);
        printf("I2C1 MUERTO. Codigo error HAL: %lu\r\n", err1);
    }

    // Test I2C3 (Volante 2 en PA7 / PB4)
    if(HAL_I2C_Mem_Read(&hi2c3, 0x6C, 0x0C, I2C_MEMADD_SIZE_8BIT, test_buffer, 2, 100) == HAL_OK) {
        printf(">>> I2C3 (PA7/PB4) VIVO <<< Angulo: %d\r\n", (test_buffer[0] << 8) | test_buffer[1]);
    } else {
        uint32_t err3 = HAL_I2C_GetError(&hi2c3);
        printf("I2C3 MUERTO. Codigo error HAL: %lu\r\n", err3);
    }

    printf("--------------------------------\r\n");
    HAL_Delay(3000);
  // Encender PWM de Motores en 0% (TIM2)
  HAL_TIM_PWM_Start(&htim2, TIM_CHANNEL_1); // PA0
  HAL_TIM_PWM_Start(&htim2, TIM_CHANNEL_2); // PA1
  TIM2->CCR1 = 0; // Motor 1 apagado (0 a 1000)
  TIM2->CCR2 = 0; // Motor 2 apagado

  // Iniciar escucha por Serial (UART2)
  HAL_UART_Receive_IT(&huart2, &rx_byte, 1);

  // Lectura inicial de AS5600
  angulo_anterior_1 = Leer_Angulo_AS5600(&hi2c1, 1512);
  angulo_anterior_2 = Leer_Angulo_AS5600(&hi2c3, 1512);
  /* USER CODE END 2 */

  /* Infinite loop */
  /* USER CODE BEGIN WHILE */
  while (1)
    {


	    // 1. BOTONES
	    btn_lim = readButton(GPIOB, GPIO_PIN_1, &bl_last);
	    btn_su  = readButton(GPIOA, GPIO_PIN_3, &bs_last);
	    btn1    = readButton(GPIOA, GPIO_PIN_4, &b1_last);
	    btn2    = readButton(GPIOA, GPIO_PIN_6, &b2_last);
	    btn3    = readButton(GPIOA, GPIO_PIN_8, &b3_last);
	    btn4    = readButton(GPIOB, GPIO_PIN_0, &b4_last);

		// 2. VOLANTES (AS5600)
	    uint16_t angulo_actual_1 = Leer_Angulo_AS5600(&hi2c1, angulo_anterior_1);
		uint16_t angulo_actual_2 = Leer_Angulo_AS5600(&hi2c3, angulo_anterior_2);

		if(angulo_actual_1 == 1512 && angulo_actual_2 == 1512) err_encoder = 1;
		else err_encoder = 0;

		int16_t diff_1 = angulo_actual_1 - angulo_anterior_1;
		int16_t diff_2 = angulo_actual_2 - angulo_anterior_2;

		if (diff_1 > 2048) diff_1 -= 4096;
		if (diff_1 < -2048) diff_1 += 4096;
		if (diff_2 > 2048) diff_2 -= 4096;
		if (diff_2 < -2048) diff_2 += 4096;

		angulo_anterior_1 = angulo_actual_1;
		angulo_anterior_2 = angulo_actual_2;

		enc1_accumulator += diff_1;
		enc2_accumulator += diff_2;

		if (enc1_accumulator >= MAGNETIC_THRESHOLD_1) {
					enc1_send = 1; enc1_accumulator -= MAGNETIC_THRESHOLD_1; activate = 1;
		} else if (enc1_accumulator <= -MAGNETIC_THRESHOLD_1) {
			enc1_send = -1; enc1_accumulator += MAGNETIC_THRESHOLD_1; activate = 1;
		}

		if (enc2_accumulator >= MAGNETIC_THRESHOLD_2) {
			enc2_send = 1; enc2_accumulator -= MAGNETIC_THRESHOLD_2; activate = 1;
		} else if (enc2_accumulator <= -MAGNETIC_THRESHOLD_2) {
			enc2_send = -1; enc2_accumulator += MAGNETIC_THRESHOLD_2; activate = 1;
		}

	  // 3. RECEPCIÓN UNITY -> VIBRACIÓN
		if(mensaje_completo == 1) {
			int fuerza_motor = 0;
			if(sscanf((char*)rx_buffer, "V1:%d", &fuerza_motor) == 1) {
				TIM2->CCR1 = fuerza_motor; // Enciende el motor
				  tiempo_inicio_vibracion = HAL_GetTick(); // Guarda la hora actual
				  vibrando = 1; // Avisa que está vibrando
			}
			else if(sscanf((char*)rx_buffer, "V2:%d", &fuerza_motor) == 1) {
				TIM2->CCR2 = fuerza_motor;
				  tiempo_inicio_vibracion = HAL_GetTick();
				  vibrando = 1;
			}
			mensaje_completo = 0;
		}

		  // 4. EL AUTO-APAGADO DEL VIBRADOR (Cooldown de 1 segundo)
		  if(vibrando == 1 && (HAL_GetTick() - tiempo_inicio_vibracion >= 1000)) {
			  TIM2->CCR1 = 0; // Apaga Motor 1
			  TIM2->CCR2 = 0; // Apaga Motor 2
			  vibrando = 0;   // Resetea el estado
		  }
		// 5. ENVÍO DE DATOS A UNITY
		  if (btn_lim || btn_su || btn1 || btn2 || btn3 || btn4 || activate == 1) {

			  // Limitador a +1 / -1 solo para Inserción
			  if (encoder_insercion > 1) encoder_insercion = 1;
			  else if (encoder_insercion < -1) encoder_insercion = -1;

			  //FORMATO DE ENVÍO ORDENADO
			  // Lim:0 Su:0 B1:0 B2:0 B3:0 B4:0 E1:0 E2:0 INS:0
			  printf("Lim:%d Su:%d B1:%d B2:%d B3:%d B4:%d E1:%d E2:%d INS:%ld\r\n",
					 btn_lim, btn_su, btn1, btn2, btn3, btn4,
					 enc2_send, enc1_send, -encoder_insercion);

			  activate = 0;
			  enc1_send = 0;
			  enc2_send = 0;
			  encoder_insercion = 0;
		  }

		  HAL_Delay(10);
	  }
    /* USER CODE END WHILE */

    /* USER CODE BEGIN 3 */
  /* USER CODE END 3 */
}

/**
  * @brief System Clock Configuration
  * @retval None
  */
void SystemClock_Config(void)
{
  RCC_OscInitTypeDef RCC_OscInitStruct = {0};
  RCC_ClkInitTypeDef RCC_ClkInitStruct = {0};

  /** Configure the main internal regulator output voltage
  */
  if (HAL_PWREx_ControlVoltageScaling(PWR_REGULATOR_VOLTAGE_SCALE1) != HAL_OK)
  {
    Error_Handler();
  }

  /** Initializes the RCC Oscillators according to the specified parameters
  * in the RCC_OscInitTypeDef structure.
  */
  RCC_OscInitStruct.OscillatorType = RCC_OSCILLATORTYPE_MSI;
  RCC_OscInitStruct.MSIState = RCC_MSI_ON;
  RCC_OscInitStruct.MSICalibrationValue = 0;
  RCC_OscInitStruct.MSIClockRange = RCC_MSIRANGE_6;
  RCC_OscInitStruct.PLL.PLLState = RCC_PLL_ON;
  RCC_OscInitStruct.PLL.PLLSource = RCC_PLLSOURCE_MSI;
  RCC_OscInitStruct.PLL.PLLM = 1;
  RCC_OscInitStruct.PLL.PLLN = 16;
  RCC_OscInitStruct.PLL.PLLP = RCC_PLLP_DIV7;
  RCC_OscInitStruct.PLL.PLLQ = RCC_PLLQ_DIV2;
  RCC_OscInitStruct.PLL.PLLR = RCC_PLLR_DIV2;
  if (HAL_RCC_OscConfig(&RCC_OscInitStruct) != HAL_OK)
  {
    Error_Handler();
  }

  /** Initializes the CPU, AHB and APB buses clocks
  */
  RCC_ClkInitStruct.ClockType = RCC_CLOCKTYPE_HCLK|RCC_CLOCKTYPE_SYSCLK
                              |RCC_CLOCKTYPE_PCLK1|RCC_CLOCKTYPE_PCLK2;
  RCC_ClkInitStruct.SYSCLKSource = RCC_SYSCLKSOURCE_PLLCLK;
  RCC_ClkInitStruct.AHBCLKDivider = RCC_SYSCLK_DIV1;
  RCC_ClkInitStruct.APB1CLKDivider = RCC_HCLK_DIV1;
  RCC_ClkInitStruct.APB2CLKDivider = RCC_HCLK_DIV1;

  if (HAL_RCC_ClockConfig(&RCC_ClkInitStruct, FLASH_LATENCY_1) != HAL_OK)
  {
    Error_Handler();
  }
}

/* USER CODE BEGIN 4 */

/* USER CODE END 4 */

/**
  * @brief  This function is executed in case of error occurrence.
  * @retval None
  */
void Error_Handler(void)
{
  /* USER CODE BEGIN Error_Handler_Debug */
  /* User can add his own implementation to report the HAL error return state */
  __disable_irq();
  while (1)
  {
  }
  /* USER CODE END Error_Handler_Debug */
}
#ifdef USE_FULL_ASSERT
/**
  * @brief  Reports the name of the source file and the source line number
  *         where the assert_param error has occurred.
  * @param  file: pointer to the source file name
  * @param  line: assert_param error line source number
  * @retval None
  */
void assert_failed(uint8_t *file, uint32_t line)
{
  /* USER CODE BEGIN 6 */
  /* User can add his own implementation to report the file name and line number,
     ex: printf("Wrong parameters value: file %s on line %d\r\n", file, line) */
  /* USER CODE END 6 */
}
#endif /* USE_FULL_ASSERT */
