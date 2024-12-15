import { Component } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';

@Component({
    selector: 'app-stat-row',
    imports: [ReactiveFormsModule],
    templateUrl: './stat-row.component.html',
    styleUrl: './stat-row.component.css'
})
export class StatRowComponent {
  statForm = new FormGroup({
    statName: new FormControl(''),
    minValue: new FormControl(0),
    maxValue: new FormControl(0),
    weight: new FormControl(0),
  })

  test() {
    alert(this.statForm.value.minValue);
  }
}
